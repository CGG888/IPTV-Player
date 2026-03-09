using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using LibmpvIptvClient.Models;
using LibmpvIptvClient.Services;

namespace LibmpvIptvClient.Tests
{
    [TestClass]
    public class EpgMappingTests
    {
        [TestMethod]
        public void GetProgramAt_MidnightBoundary()
        {
            var svc = new EpgService();
            var tvg = "CCTV6";
            var list = new List<EpgProgram>
            {
                new EpgProgram{ Title="A", Start=new DateTime(2026,3,9,23,30,0), End=new DateTime(2026,3,10,0,15,0)},
                new EpgProgram{ Title="B", Start=new DateTime(2026,3,10,0,15,0), End=new DateTime(2026,3,10,1,0,0)},
            };
            svc.SeedPrograms(tvg, list, "CCTV6-电影");
            var hit1 = svc.GetProgramAt(tvg, new DateTime(2026,3,9,23,45,0), "CCTV6-电影");
            var hit2 = svc.GetProgramAt(tvg, new DateTime(2026,3,10,0,20,0), "CCTV6-电影");
            Assert.AreEqual("A", hit1?.Title);
            Assert.AreEqual("B", hit2?.Title);
        }

        [TestMethod]
        public void GetProgramAt_RangeValidation()
        {
            var svc = new EpgService();
            var tvg = "CCTV1";
            svc.SeedPrograms(tvg, new List<EpgProgram>(), "CCTV1-综合");
            Assert.IsNull(svc.GetProgramAt(tvg, new DateTime(1979,12,31,23,59,0), "CCTV1-综合"));
            Assert.IsNull(svc.GetProgramAt(tvg, new DateTime(2038,1,1,0,0,0), "CCTV1-综合"));
            // Valid edges
            Assert.IsNull(svc.GetProgramAt(tvg, new DateTime(1980,1,1,0,0,0), "CCTV1-综合")); // no programs but valid call
            Assert.IsNull(svc.GetProgramAt(tvg, new DateTime(2037,12,31,23,59,0), "CCTV1-综合"));
        }

        [TestMethod]
        public void GetProgramsByHour_CachesSegments()
        {
            var svc = new EpgService();
            var tvg = "T";
            var baseDay = new DateTime(2026,3,10,0,0,0);
            var list = new List<EpgProgram>
            {
                new EpgProgram{ Title="H0", Start=baseDay.AddMinutes(10), End=baseDay.AddMinutes(55)},
                new EpgProgram{ Title="H1", Start=baseDay.AddHours(1).AddMinutes(5), End=baseDay.AddHours(1).AddMinutes(50)},
            };
            svc.SeedPrograms(tvg, list, "T");
            var seg0 = svc.GetProgramsByHour(tvg, baseDay, "T");
            var seg1 = svc.GetProgramsByHour(tvg, baseDay.AddHours(1), "T");
            Assert.AreEqual(1, seg0.Count);
            Assert.AreEqual(1, seg1.Count);
            Assert.AreEqual("H0", seg0[0].Title);
            Assert.AreEqual("H1", seg1[0].Title);
        }
    }
}
