using System;
namespace DataPemilihCrawler.Model
{
    public class Wilayah
    {
        public int jmlTps { get; set; }
        public int jmlPemilihLaki { get; set; }
        public int jmlPemilihPerempuan { get; set; }
        public int jmlPemilihKosong { get; set; }
        public int totalPemilih { get; set; }
        public int jmlPemilihPemulaLaki { get; set; }
        public int jmlPemilihPemulaPerempuan { get; set; }
        public int totalPemilihPemula { get; set; }
        public double persenPemilihPemula { get; set; }
        public int jmlDifabel1 { get; set; }
        public int jmlDifabel2 { get; set; }
        public int jmlDifabel3 { get; set; }
        public int jmlDifabel4 { get; set; }
        public int jmlDifabel5 { get; set; }
        public int totalDifabel { get; set; }
        public double persenDifabel { get; set; }
    }
}
