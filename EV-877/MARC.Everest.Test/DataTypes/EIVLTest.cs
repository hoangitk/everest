﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC.Everest.DataTypes;
namespace MARC.Everest.Test.DataTypes
{
    /// <summary>
    /// Summary description for EIVLTest
    /// </summary>
    [TestClass]
    public class EIVLTest
    {
        public EIVLTest()
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
        /// Valid EIVL
        /// </summary>
        [TestMethod]
        public void EIVLValidEIVL()
        {
            // Offset of 1d
            IVL<PQ> effectiveTime = new IVL<PQ>() { High = new PQ(1, "d") };
            // Event is BeforeLunch
            CS<DomainTimingEventType> event1 = new CS<DomainTimingEventType>(DomainTimingEventType.BeforeLunch);
            
            // Create EIVL instance and pass it the effective Time and event 
            EIVL<TS> eivlInstance = new EIVL<TS>();
            //eivlInstance.Offset = effectiveTime;
            eivlInstance.Event = event1;

            // true if it validates
            Assert.IsTrue(eivlInstance.Validate());
        }
    }
}
