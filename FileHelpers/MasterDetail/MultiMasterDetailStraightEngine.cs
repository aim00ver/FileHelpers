using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FileHelpers.Events;
using FileHelpers.Options;

namespace FileHelpers.MasterDetail
{
    #region "  Delegate  "

    /// <summary>
    /// Delegate that determines the Type of the current record (Master, Detail, Skip)
    /// </summary>
    /// <param name="recordString">The string of the current record.</param>
    /// <param name="engine">The engine that calls the selector.</param>
    /// <returns>The action used for the current record (Master, Detail, Skip)</returns>
    public delegate Type MultiRecordTypeSelector(MultiMasterDetailStraightEngine engine, string recordString);

    #endregion

    public class DetailSelector
    {
        public readonly Type Detail;
        public readonly string Start;
        public readonly string Separator;
        public readonly string End;

        public DetailSelector(Type detail, string start, string separator, string end)
        {
            Detail = detail;
            Start = start;
            Separator = separator;
            End = end;
        }
    }

    public class MasterMultiDetailsInfo
    {
        public readonly Type Master;
        public readonly ISet<DetailSelector> Details;

        public MasterMultiDetailsInfo(Type master, ISet<DetailSelector> details)
        {
            Master = master;
            Details = details;
        }
    }

    /// <summary>
    /// <para>This engine allows you to parse and write files that contain
    /// records of different types and that are in a linear relationship</para>
    /// <para>(for Master-Detail check the <see cref="MasterDetailEngine"/>)</para>
    /// </summary>
    [DebuggerDisplay(
        "MultiRecordEngine for types: {ListTypes()}. ErrorMode: {ErrorManager.ErrorMode.ToString()}. Encoding: {Encoding.EncodingName}"
        )]
    public sealed class MultiMasterDetailStraightEngine
        :
            EventEngineBase<object>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IRecordInfo[] mMultiRecordInfo;

        private readonly RecordOptions[] mMultiRecordOptions;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, IRecordInfo> mRecordInfoHash;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, ISet<DetailSelector>> mDetailSelectorHash;
        private readonly ISet<RecordOptions> mDetailRecordOptions;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MultiRecordTypeSelector mRecordSelector;

        /// <summary>
        /// The Selector used by the engine in Read operations to determine the Type to use.
        /// </summary>
        public MultiRecordTypeSelector RecordSelector
        {
            get { return mRecordSelector; }
            set { mRecordSelector = value; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MasterMultiDetailsInfo[] mTypes;

        #region "  Constructor  "

        /// <summary>Create a new instance of the MultiRecordEngine</summary>
        /// <param name="recordTypes">The Types of the records that this engine can handle.</param>
        public MultiMasterDetailStraightEngine(MasterMultiDetailsInfo[] recordTypes)
            : this(null, recordTypes)
        {
        }

        /// <summary>Create a new instance of the MultiRecordEngine</summary>
        /// <param name="recordTypes">The Types of the records that this engine can handle.</param>
        /// <param name="recordSelector">
        /// Used only in read operations. The selector indicates to the engine
        /// what Type to use in each read line.
        /// </param>
        public MultiMasterDetailStraightEngine(MultiRecordTypeSelector recordSelector, MasterMultiDetailsInfo[] recordTypes)
            : base(GetFirstType(recordTypes))
        {
            mTypes = recordTypes;
            mMultiRecordInfo = new IRecordInfo[mTypes.Length];
            mRecordInfoHash = new Dictionary<Type, IRecordInfo>();
            mMultiRecordOptions = new RecordOptions[mTypes.Length];
            mDetailSelectorHash = new Dictionary<Type, ISet<DetailSelector>>();
            mDetailRecordOptions = new HashSet<RecordOptions>();

            for (int i = 0; i < mTypes.Length; i++)
            {
                if (mTypes[i] == null)
                    throw new BadUsageException("The type at index " + i + " is null.");

                if (mRecordInfoHash.ContainsKey(mTypes[i].Master))
                {
                    throw new BadUsageException("The type '" + mTypes[i].Master.Name +
                                                " is already in the engine. You can't pass the same type twice to the constructor.");
                }

                mMultiRecordInfo[i] = FileHelpers.RecordInfo.Resolve(mTypes[i].Master);
                mMultiRecordOptions[i] = CreateRecordOptionsCore(mMultiRecordInfo[i]);

                mRecordInfoHash.Add(mTypes[i].Master, mMultiRecordInfo[i]);

                foreach (var item in mTypes[i].Details)
                {
                    var detailRecordInfo = FileHelpers.RecordInfo.Resolve(item.Detail);
                    mRecordInfoHash.Add(item.Detail, detailRecordInfo);
                    mDetailRecordOptions.Add(CreateRecordOptionsCore(detailRecordInfo));
                }
                mDetailSelectorHash.Add(mTypes[i].Master, mTypes[i].Details);
            }
            mRecordSelector = recordSelector;
        }

        #endregion

        #region "  ReadFile  "

        /// <summary>
        /// Reads a file and returns the records.
        /// </summary>
        /// <param name="fileName">The file with the records.</param>
        /// <returns>The read records of different types all mixed.</returns>
        public MasterMultiDetails[] ReadFile(string fileName)
        {
            using (var fs = new StreamReader(fileName, mEncoding, true, DefaultReadBufferSize))
                return ReadStream(fs);
        }

        #endregion

        #region "  ReadStream  "

        /// <summary>
        /// Read an array of objects from a stream
        /// </summary>
        /// <param name="reader">Stream we are reading from</param>
        /// <returns>Array of objects</returns>
        public MasterMultiDetails[] ReadStream(TextReader reader)
        {
            return ReadStream(new NewLineDelimitedRecordReader(reader));
        }

        /// <include file='MultiRecordEngine.docs.xml' path='doc/ReadStream/*'/>
        public MasterMultiDetails[] ReadStream(IRecordReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader", "The reader of the Stream can´t be null");

            if (mRecordSelector == null)
            {
                throw new BadUsageException(
                    "The Recordselector can´t be null, please pass a not null Selector in the constructor.");
            }

            ResetFields();
            mHeaderText = String.Empty;
            mFooterText = String.Empty;

            var resArray = new List<MasterMultiDetails>();

            using (var freader = new ForwardReader(reader, mMultiRecordInfo[0].IgnoreLast))
            {
                freader.DiscardForward = true;

                mLineNumber = 1;

                var completeLine = freader.ReadNextLine();
                var currentLine = completeLine;

                if (MustNotifyProgress) // Avoid object creation
                    OnProgress(new ProgressEventArgs(0, -1));

                int currentRecord = 0;

                if (mMultiRecordInfo[0].IgnoreFirst > 0)
                {
                    for (int i = 0; i < mMultiRecordInfo[0].IgnoreFirst && currentLine != null; i++)
                    {
                        mHeaderText += currentLine + StringHelper.NewLine;
                        currentLine = freader.ReadNextLine();
                        mLineNumber++;
                    }
                }

                bool byPass = false;
                MasterMultiDetails record = null;
                var tmpDetails = new ArrayList();
                var line = new LineInfo(currentLine)
                {
                    mReader = freader
                };

                while (currentLine != null)
                {
                    Type currType = null;

                    try
                    {
                        mTotalRecords++;
                        currentRecord++;

                        var skip = false;
                        try
                        {
                            currType = mRecordSelector(this, currentLine);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Selector failed to process correctly", ex);
                        }

                        if (currType != null)
                        {
                            var info = (RecordInfo) mRecordInfoHash[currType];
                            if (info == null)
                            {
                                throw new BadUsageException("A record is of type '" + currType.Name +
                                                            "' which this engine is not configured to handle. Try adding this type to the constructor.");
                            }
                            record = new MasterMultiDetails();

                            var lastMaster = info.Operations.CreateRecordHandler();
                            var masterEndIndex = currentLine.Length;
                            foreach (var detailSelector in mDetailSelectorHash[currType])
                            {
                                var detailIndex = currentLine.IndexOf(detailSelector.Start);
                                if (detailIndex > 0 && detailIndex < masterEndIndex)
                                    masterEndIndex = detailIndex;
                            }
                            var lineMaster = currentLine.Substring(0, masterEndIndex);

                            line.ReLoad(lineMaster);

                            if (MustNotifyProgress) // Avoid object creation
                                OnProgress(new ProgressEventArgs(currentRecord, -1));

                            BeforeReadEventArgs<object> e = null;
                            if (MustNotifyRead) // Avoid object creation
                            {
                                e = new BeforeReadEventArgs<object>(this, lastMaster, lineMaster, LineNumber);
                                skip = OnBeforeReadRecord(e);
                                if (e.RecordLineChanged)
                                    line.ReLoad(e.RecordLine);
                            }

                            if (skip == false)
                            {
                                var values = new object[info.FieldCount];
                                if (info.Operations.StringToRecord(lastMaster, line, values, mErrorManager))
                                {
                                    if (MustNotifyRead) // Avoid object creation
                                        skip = OnAfterReadRecord(lineMaster, lastMaster, e.RecordLineChanged, LineNumber);

                                    if (skip == false)
                                        record.Master = lastMaster;
                                }
                                var detailLocations = new Dictionary<Type, Tuple<int, int>>();
                                if (currentLine.Length > lineMaster.Length)
                                {
                                    var detailsLine = currentLine.Substring(masterEndIndex);
                                    foreach (var detailSelector in mDetailSelectorHash[currType])
                                    {
                                        var detailStartIndex = detailsLine.IndexOf(detailSelector.Start) + 1;
                                        if (detailStartIndex < 1)
                                            continue;
                                        var detailEndIndex = detailsLine.IndexOf(detailSelector.End, detailStartIndex);
                                        if (detailEndIndex < 0)
                                            detailEndIndex = detailsLine.Length;

                                        detailLocations.Add(detailSelector.Detail, new Tuple<int, int>
                                            (masterEndIndex + 1 + detailStartIndex, masterEndIndex + 1 + detailEndIndex - detailStartIndex));

                                        var lineDetails = detailsLine.Substring(detailStartIndex, detailEndIndex - detailStartIndex);
                                        var detailInfo = (RecordInfo)mRecordInfoHash[detailSelector.Detail];
                                        var valuesDetail = new object[detailInfo.FieldCount];
                                        tmpDetails.Clear();
                                        foreach (var lineDetail in lineDetails.Split(new []{ detailSelector.Separator }, StringSplitOptions.RemoveEmptyEntries))
                                        {
                                            try
                                            {
                                                line.ReLoad(lineDetail);
                                                var lastChild = detailInfo.Operations.StringToRecord(line, valuesDetail, ErrorManager);
                                                if (lastChild != null)
                                                    tmpDetails.Add(lastChild);
                                            }
                                            catch (Exception ex)
                                            {
                                                switch (mErrorManager.ErrorMode)
                                                {
                                                    case ErrorMode.ThrowException:
                                                        throw;
                                                    case ErrorMode.IgnoreAndContinue:
                                                        break;
                                                    case ErrorMode.SaveAndContinue:
                                                        var err = new ErrorInfo
                                                        {
                                                            mErrorType = ErrorInfo.ErrorTypeEnum.Detail,
                                                            mFailedOnType = detailSelector.Detail,
                                                            mLineNumber = mLineNumber,
                                                            mExceptionInfo = ex,
                                                            mRecordString = currentLine,
                                                            mStart = detailLocations[detailSelector.Detail].Item1,
                                                            mLength = detailLocations[detailSelector.Detail].Item2
                                                        };
                                                        //							err.mColumnNumber = mColumnNum;

                                                        mErrorManager.AddError(err);
                                                        throw;//fail all record if detail failed
                                                }
                                            }
                                        }
                                        record.Details.Add(detailSelector.Detail, tmpDetails.ToArray());
                                    }

                                    OnAfterReadRecordWithDetails(currentLine, record, e.RecordLineChanged, LineNumber,
                                        new Tuple<int, int>(0, masterEndIndex), detailLocations);
                                }
                                if (skip == false)
                                    resArray.Add(record);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        switch (mErrorManager.ErrorMode)
                        {
                            case ErrorMode.ThrowException:
                                byPass = true;
                                throw;
                            case ErrorMode.IgnoreAndContinue:
                                break;
                            case ErrorMode.SaveAndContinue:
                                if (mErrorManager.Errors.Where(err => err.LineNumber == freader.LineNumber).Count() == 0)//error in master
                                {
                                    var err = new ErrorInfo
                                    {
                                        mErrorType = ErrorInfo.ErrorTypeEnum.Master,
                                        mFailedOnType = currType,
                                        mLineNumber = freader.LineNumber,
                                        mExceptionInfo = ex,
                                        mRecordString = currentLine,
                                        mStart = 0,
                                        mLength = (currentLine == null ? 0 : currentLine.Length)
                                    };
                                    mErrorManager.AddError(err);
                                }
                                break;
                        }
                    }
                    finally
                    {
                        if (byPass == false)
                        {
                            currentLine = freader.ReadNextLine();
                            completeLine = currentLine;
                            mLineNumber = freader.LineNumber;
                        }
                    }
                }

                if (mMultiRecordInfo[0].IgnoreLast > 0)
                    mFooterText = freader.RemainingText;
            }
            return resArray.ToArray();
        }

        #endregion

        #region "  ReadString  "

        /// <include file='MultiRecordEngine.docs.xml' path='doc/ReadString/*'/>
        public MasterMultiDetails[] ReadString(string source)
        {
            var reader = new InternalStringReader(source);
            MasterMultiDetails[] res = ReadStream(reader);
            reader.Close();
            return res;
        }

        #endregion

        #region "  WriteFile  "

        /// <include file='MultiRecordEngine.docs.xml' path='doc/WriteFile/*'/>
        public void WriteFile(string fileName, IEnumerable<MasterMultiDetails> records)
        {
            WriteFile(fileName, records, -1);
        }

        /// <include file='MultiRecordEngine.docs.xml' path='doc/WriteFile2/*'/>
        public void WriteFile(string fileName, IEnumerable<MasterMultiDetails> records, int maxRecords)
        {
            using (var fs = new StreamWriter(fileName, false, mEncoding, DefaultWriteBufferSize))
            {
                WriteStream(fs, records, maxRecords);
                fs.Close();
            }
        }

        #endregion

        #region "  WriteStream  "

        /// <summary>
        /// Write the records to a file
        /// </summary>
        /// <param name="writer">Where data is written</param>
        /// <param name="records">records to write to the file</param>
        public void WriteStream(TextWriter writer, IEnumerable<MasterMultiDetails> records)
        {
            WriteStream(writer, records, -1);
        }

        /// <include file='MultiRecordEngine.docs.xml' path='doc/WriteStream2/*'/>
        public void WriteStream(TextWriter writer, IEnumerable<MasterMultiDetails> records, int maxRecords)
        {
            if (writer == null)
                throw new ArgumentNullException("writer", "The writer of the Stream can be null");

            if (records == null)
                throw new ArgumentNullException("records", "The records can be null. Try with an empty array.");

            ResetFields();

            if (!string.IsNullOrEmpty(mHeaderText))
            {
                if (mHeaderText.EndsWith(StringHelper.NewLine))
                    writer.Write(mHeaderText);
                else
                    writer.WriteLine(mHeaderText);
            }

            string currentLine = null;

            int max = maxRecords;

            if (records is IList)
            {
                max = Math.Min(max < 0
                    ? int.MaxValue
                    : max,
                    ((IList) records).Count);
            }


            if (MustNotifyProgress) // Avoid object creation
                OnProgress(new ProgressEventArgs(0, max));

            int recIndex = 0;

            foreach (var rec in records)
            {
                if (recIndex == maxRecords)
                    break;
                try
                {
                    if (rec == null)
                        throw new BadUsageException("The record at index " + recIndex + " is null.");

                    bool skip = false;

                    if (MustNotifyProgress) // Avoid object creation
                        OnProgress(new ProgressEventArgs(recIndex + 1, max));

                    if (MustNotifyWrite)
                        skip = OnBeforeWriteRecord(rec, LineNumber);

                    var info = (IRecordInfo) mRecordInfoHash[rec.Master.GetType()];

                    if (info == null)
                    {
                        throw new BadUsageException("The record at index " + recIndex + " is of type '" +
                                                    rec.GetType().Name +
                                                    "' and the engine dont handle this type. You can add it to the constructor.");
                    }

                    if (skip == false)
                    {
                        currentLine = info.Operations.RecordToString(rec.Master);

                        if (MustNotifyWrite)
                            currentLine = OnAfterWriteRecord(currentLine, rec.Master);

                        foreach (var details in rec.Details)
                        {
                            var detailsInfo = mRecordInfoHash[details.Key];
                            var detailsSelector = mDetailSelectorHash[rec.Master.GetType()].FirstOrDefault(x => x.Detail == details.Key);
                            if (detailsInfo == null || detailsSelector == null)
                            {
                                throw new BadUsageException("The record at index " + recIndex + " is of type '" +
                                                    details.Key.Name +
                                                    "' and the engine dont handle this type. You can add it to the constructor.");
                            }
                            currentLine += detailsSelector.Start;
                            for (var d = 0; d < details.Value.Length; d++)
                            {
                                if (d > 0) 
                                    currentLine += detailsSelector.Separator;
                                currentLine += detailsInfo.Operations.RecordToString(details.Value[d]);
                            }
                            currentLine += detailsSelector.End;
                        }
                        writer.WriteLine(currentLine);
                    }
                }
                catch (Exception ex)
                {
                    switch (mErrorManager.ErrorMode)
                    {
                        case ErrorMode.ThrowException:
                            throw;
                        case ErrorMode.IgnoreAndContinue:
                            break;
                        case ErrorMode.SaveAndContinue:
                            var err = new ErrorInfo
                            {
                                mLineNumber = mLineNumber,
                                mExceptionInfo = ex,
                                mRecordString = currentLine
                            };
                            mErrorManager.AddError(err);
                            break;
                    }
                }
                recIndex++;
            }

            mTotalRecords = recIndex;

            if (!string.IsNullOrEmpty(mFooterText))
            {
                if (mFooterText.EndsWith(StringHelper.NewLine))
                    writer.Write(mFooterText);
                else
                    writer.WriteLine(mFooterText);
            }
        }

        #endregion

        #region "  WriteString  "

        /// <include file='MultiRecordEngine.docs.xml' path='doc/WriteString/*'/>
        public string WriteString(IEnumerable<MasterMultiDetails> records)
        {
            return WriteString(records, -1);
        }

        /// <include file='MultiRecordEngine.docs.xml' path='doc/WriteString2/*'/>
        public string WriteString(IEnumerable<MasterMultiDetails> records, int maxRecords)
        {
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);
            WriteStream(writer, records, maxRecords);
            string res = writer.ToString();
            writer.Close();
            return res;
        }

        #endregion

        #region "  AppendToFile  "

        /// <include file='MultiRecordEngine.docs.xml' path='doc/AppendToFile1/*'/>
        public void AppendToFile(string fileName, object record)
        {
            AppendToFile(fileName, new[] {record});
        }

        /// <include file='MultiRecordEngine.docs.xml' path='doc/AppendToFile2/*'/>
        public void AppendToFile(string fileName, IEnumerable<MasterMultiDetails> records)
        {
            using (
                TextWriter writer = StreamHelper.CreateFileAppender(fileName,
                    mEncoding,
                    true,
                    false,
                    DefaultWriteBufferSize))
            {
                mHeaderText = String.Empty;
                mFooterText = String.Empty;

                WriteStream(writer, records);
                writer.Close();
            }
        }

        #endregion

        private static Type GetFirstType(MasterMultiDetailsInfo[] types)
        {
            if (types == null)
                throw new BadUsageException("A null Type[] is not valid for the MultiRecordEngine.");
            if (types.Length == 0)
                throw new BadUsageException("An empty Type[] is not valid for the MultiRecordEngine.");
            /*if (types.Length == 1)
            {
                throw new BadUsageException(
                    "You only provided one type to the engine constructor. You need 2 or more types, for one type you can use the FileHelperEngine.");
            }*/
            return types[0].Master;
        }

    }
}
