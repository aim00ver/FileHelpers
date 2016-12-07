using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FileHelpers.Dynamic;
using FileHelpers.MasterDetail;
using NUnit.Framework;
using MasterDetails = FileHelpers.MasterDetail.MasterDetails<object, object>;

namespace FileHelpers.Tests.MasterDetail
{
    [TestFixture]
    public class MultiMasterDetailStraight
    {

        [Test]
        public void DynamicDetailStraightWrite()
        {
            var masterType = GetDynamicMasterType();
            var detailType = GetDynamicDetailType();

            var detailsInfo = new HashSet<DetailSelector>();
            detailsInfo.Add(new DetailSelector(detailType, "#", "|", "#"));
            var masterDetEng = new MultiMasterDetailStraightEngine(new[] { new MasterMultiDetailsInfo(masterType, detailsInfo) });

            var records = new List<MasterMultiDetails>();

            // Create the master detail item
            var record = new MasterMultiDetails();
            records.Add(record);

            // Create the master object
            var mObj = masterType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
            masterType.GetField("Code").SetValue(mObj, "ALFKI");
            masterType.GetField("Field1").SetValue(mObj, new DateTime(2016, 04, 15));
            masterType.GetField("Field2").SetValue(mObj, "qwe qwe");
            masterType.GetField("Field3").SetValue(mObj, 100000);
            record.Master = mObj;

            // Create the details object
            var dObj1 = detailType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
            detailType.GetField("Field1").SetValue(dObj1, new DateTime(2015, 3, 16));
            detailType.GetField("Field2").SetValue(dObj1, "fbfkblnbi");
            detailType.GetField("Field3").SetValue(dObj1, 15);

            var dObj2 = detailType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
            detailType.GetField("Field1").SetValue(dObj2, new DateTime(2015, 5, 14));
            detailType.GetField("Field2").SetValue(dObj2, "cvbnuilou");
            detailType.GetField("Field3").SetValue(dObj2, 29);

            record.Details.Add(detailType, new [] { dObj1, dObj2 });

            // Create the master detail item
            var record2 = new MasterMultiDetails();
            records.Add(record2);

            // Create the master object
            var mObj2 = masterType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
            masterType.GetField("Code").SetValue(mObj2, "ANATR");
            masterType.GetField("Field1").SetValue(mObj2, new DateTime(2016, 05, 24));
            masterType.GetField("Field2").SetValue(mObj2, "ghj uyi");
            masterType.GetField("Field3").SetValue(mObj2, 988);
            record2.Master = mObj2;

            // Create the details object
            var dObj12 = detailType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
            detailType.GetField("Field1").SetValue(dObj12, new DateTime(2016, 7, 4));
            detailType.GetField("Field2").SetValue(dObj12, "jj fjfj");
            detailType.GetField("Field3").SetValue(dObj12, 789);

            var dObj22 = detailType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
            detailType.GetField("Field1").SetValue(dObj22, new DateTime(2016, 6, 24));
            detailType.GetField("Field2").SetValue(dObj22, "rtrxfg thgt");
            detailType.GetField("Field3").SetValue(dObj22, 555);

            var dObj23 = detailType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
            detailType.GetField("Field1").SetValue(dObj23, new DateTime(2016, 1, 6));
            detailType.GetField("Field2").SetValue(dObj23, "fgfffffff");
            detailType.GetField("Field3").SetValue(dObj23, 1055);

            record2.Details.Add(detailType, new[] { dObj12, dObj22, dObj23 });
            // And now write it to a file

            masterDetEng.WriteFile("D:/_Proj/myMMDSfile.txt", records.ToArray());
        }

        [Test]
        public void DynamicDetailStraightRead()
        {
            var masterType = GetDynamicMasterType();
            var detailType = GetDynamicDetailType();

            var detailsInfo = new HashSet<DetailSelector>();
            detailsInfo.Add(new DetailSelector(detailType, "#", "|", "#"));
            var masterDetEng = new MultiMasterDetailStraightEngine(new[] { new MasterMultiDetailsInfo(masterType, detailsInfo) });

            masterDetEng.RecordSelector =
                (eng, recordString) =>
                {
                    if (Char.IsLetter(recordString[0]))
                        return masterType;

                    return null;
                };

            MasterMultiDetails[] res = masterDetEng.ReadFile("D:/_Proj/myMMDSfile.txt");

            Assert.AreEqual(2, res.Length);

            Assert.AreEqual(2, masterDetEng.TotalRecords);

            Assert.AreEqual(2, res[0].Details[detailType].Length);
            Assert.AreEqual(3, res[1].Details[detailType].Length);
            
        }

        private Type GetDynamicMasterType()
        {
            var masterCb = new DelimitedClassBuilder("CustomDynamicType", ";");
            //masterCb.IgnoreFirstLines = 1;
            masterCb.IgnoreEmptyLines = true;

            masterCb.AddField("Code", typeof(string));

            masterCb.AddField("Field1", typeof(DateTime));
            masterCb.LastField.TrimMode = TrimMode.Both;
            masterCb.LastField.QuoteMode = QuoteMode.AlwaysQuoted;
            masterCb.LastField.FieldNullValue = DateTime.Today;

            masterCb.AddField("Field2", typeof(string));
            masterCb.LastField.FieldQuoted = true;
            masterCb.LastField.QuoteChar = '"';

            masterCb.AddField("Field3", typeof(int));
            return masterCb.CreateRecordClass();
        }

        private Type GetDynamicDetailType()
        {
            var detailCb = new FixedLengthClassBuilder("Customers");

            detailCb.AddField("Field1", 8, typeof(DateTime));
            detailCb.LastField.Converter.Kind = ConverterKind.Date;
            detailCb.LastField.Converter.Arg1 = "ddMMyyyy";

            detailCb.AddField("Field2", 10, typeof(string));
            //detailCb.LastField.AlignMode = AlignMode.Right;
            //detailCb.LastField.AlignChar = ' ';

            detailCb.AddField("Field3", 10, typeof(int));
            //detailCb.LastField.AlignMode = AlignMode.Right;
            //detailCb.LastField.AlignChar = '0';
            //detailCb.LastField.TrimMode = TrimMode.Both;

            return detailCb.CreateRecordClass();
        }

    }
}