﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC.Everest.DataTypes;

namespace MARC.Everest.Test.DataTypes.R2
{
    /// <summary>
    /// Unit tests for SLIST
    /// </summary>
    [TestClass]
    public class SLISTTest
    {
        public SLISTTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        /// <summary>
        /// SLIST Serialization test
        /// </summary>
        [TestMethod]
        public void SLISTSerializationTest()
        {
            SLIST<INT> slist = SLIST<INT>.CreateSLIST(
                10,
                new RTO<INT, INT>(3, 8),
                1, 2, 3, 4, 5, 6, 7
            );
            string actualXml = R2SerializationHelper.SerializeAsString(slist);
            var inti = R2SerializationHelper.ParseString<SLIST<INT>>(actualXml);
            Assert.AreEqual(slist, inti);
        }
    }
}