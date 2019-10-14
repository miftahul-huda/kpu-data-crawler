using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using DataPemilihCrawler.Model;
using Newtonsoft.Json;
using Google.Cloud.Storage.V1;

namespace DataPemilihCrawler
{
    class Program
    {
        static string GetListOfProvinceUrl = "https://pilkada2017.kpu.go.id/pemilih/dpt/1/listNasional.json";
        static string GetListOfKabupatenUrl = "https://pilkada2017.kpu.go.id/pemilih/dpt/1/{{namapropinsi}}/listDps.json";
        static string GetListOfKecamatanUrl = "https://pilkada2017.kpu.go.id/pemilih/dpt/1/{{namapropinsi}}/{{namakabupaten}}/listDps.json";
        static string GetListOfKelurahanUrl = "https://pilkada2017.kpu.go.id/pemilih/dpt/1/{{namapropinsi}}/{{namakabupaten}}/{{namakecamatan}}/listDps.json";
        static string GetListOfTpsUrl = "https://pilkada2017.kpu.go.id/pemilih/dpt/1/{{namapropinsi}}/{{namakabupaten}}/{{namakecamatan}}/{{namakelurahan}}/listDps.json";
        static string GetListOfPemilihUrl = "https://pilkada2017.kpu.go.id/pemilih/dpt/1/{{namapropinsi}}/{{namakabupaten}}/{{namakecamatan}}/{{namakelurahan}}/{{tps}}/listDps.json";
        static string FileToSave = "/Users/miftahul.huda/Projects/DataPemilihCrawler/data2017/voters.{namapropinsi}.{namakabupaten}.{namakecamatan}.{namakelurahan}.{tps}.csv";
        //static string FileToSave = "gs://huda-playground.appspot.com/voter-data/voters.{namapropinsi}.{namakabupaten}.{namakecamatan}.{namakelurahan}.{tps}.csv";


        static void Main(string[] args)
        {
            string starting_province = "";
            if(args.Length > 0)
            {
                starting_province = args[0];
                Console.WriteLine(starting_province);
            }
            List<Province> provinces = GetListOfProvince();
            bool ok = false;
            foreach(Province province in provinces)
            {
                Console.WriteLine(province.namaWilayah);
                if((starting_province.Length > 0 && starting_province.ToLower().Equals(province.namaWilayah.ToLower())) || starting_province.Length == 0)
                {
                    ok = true;
                }
                if (ok)
                {
                    List<Kabupaten> kabupatens = new List<Kabupaten>();
                    try
                    {
                        kabupatens = GetListOfKabupaten(province);
                    }
                    catch { }
                        
                    foreach (Kabupaten kabupaten in kabupatens)
                    {
                        List<Kecamatan> kecamatans = new List<Kecamatan>();

                        try
                        {
                            kecamatans = GetListOfKecamatan(kabupaten);
                        }
                        catch { }
                            
                        foreach (Kecamatan kecamatan in kecamatans)
                        {
                            List<Kelurahan> kelurahans = new List<Kelurahan>();
                            try
                            {
                                kelurahans = GetListOfKelurahan(kecamatan);
                            }
                            catch { }
                                
                            foreach (Kelurahan kelurahan in kelurahans)
                            {
                               
                                List<Tps> tps = new List<Tps>();
                                try
                                {
                                    tps = GetListOfTps(kelurahan);
                                }
                                catch { }
                                
                                foreach (Tps tps1 in tps)
                                {
                                    string info = "Province: {0}, Kabupaten: {1}, Kecamatan: {2}, Kelurahan: {3}, Tps: {4}";

                                    Console.WriteLine(info, province.namaWilayah, kabupaten.namaKabKota, kecamatan.namaKecamatan, kelurahan.namaKelurahan, tps1.tps);

                                    try
                                    {
                                        List<Voter> voters = GetListOfVoters(tps1, province.namaWilayah);
                                        Console.WriteLine("{0} voters", voters.Count);
                                        string filename = ReplaceFilename(FileToSave, tps1);
                                        Console.WriteLine("Filename : " + filename);
                                        SaveVoters(voters, filename);
                                    }
                                    catch(Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }

        static string ReplaceFilename(string filename, Tps tps)
        {
            filename = filename.Replace("{namapropinsi}", tps.namaPropinsi);
            filename = filename.Replace("{namakabupaten}", tps.namaKabKota);
            filename = filename.Replace("{namakecamatan}", tps.namaKecamatan);
            filename = filename.Replace("{namakelurahan}", tps.namaKelurahan);
            filename = filename.Replace("{tps}", tps.tps);
            filename = filename.Replace(" ", "");
            return filename;
        }

        static void WriteCSV<T>(IEnumerable<T> items, string path)
        {
            bool append = false;
            if (System.IO.File.Exists(path))
            {
                FileInfo fileInfo = new System.IO.FileInfo(path);
                if(fileInfo.Length > 0)
                {
                    append = true;
                }
            }

            Type itemType = typeof(T);
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);


            using (var writer = new StreamWriter(path, true))
            {
                if(append == false)
                    writer.WriteLine(string.Join(",", props.Select(p => p.Name)));

                foreach (var item in items)
                {
                    object[] vs = props.Select(p => p.GetValue(item, null)).ToArray();

                    for(int i = 0; i < vs.Length; i++)
                    {
                        string ss = Convert.ToString(vs[i]);
                        ss = ss.Replace(",", "");
                        ss = ss.Replace("\t", "");
                        ss = ss.Replace("\r\n", "");
                        vs[i] = ss;
                    }
                    
                    writer.WriteLine(string.Join(",", vs));
                }

                writer.Close();
            }

            
        }

        static void SaveVoters(List<Voter> voters, string filename)
        {
            WriteCSV<Voter>(voters, filename);
        }

        static List<Province> GetListOfProvince()
        {
            WebClient client = new WebClient();
            string s = client.DownloadString(GetListOfProvinceUrl);

            RootData rootData = JsonConvert.DeserializeObject<RootData>(s);
            s = JsonConvert.SerializeObject(rootData.aaData);

            List<Province> provinces = JsonConvert.DeserializeObject<List<Province>>(s);
            return provinces;
        }

        static List<Kabupaten> GetListOfKabupaten(Province province)
        {
            WebClient client = new WebClient();
            string url = GetListOfKabupatenUrl.Replace("{{namapropinsi}}", province.namaWilayah.ToUpper());
            string s = client.DownloadString(url);

            RootData rootData = JsonConvert.DeserializeObject<RootData>(s);
            s = JsonConvert.SerializeObject(rootData.aaData);

            List<Kabupaten> kabupatens = JsonConvert.DeserializeObject<List<Kabupaten>>(s);
            return kabupatens;
        }

        static List<Kecamatan> GetListOfKecamatan(Kabupaten kabupaten, string province = "")
        {
            WebClient client = new WebClient();
            string url = GetListOfKecamatanUrl.Replace("{{namakabupaten}}", kabupaten.namaKabKota);
            url = url.Replace("{{namapropinsi}}", kabupaten.namaPropinsi);
            string s = client.DownloadString(url);

            RootData rootData = JsonConvert.DeserializeObject<RootData>(s);
            s = JsonConvert.SerializeObject(rootData.aaData);

            List<Kecamatan> kecamatans = JsonConvert.DeserializeObject<List<Kecamatan>>(s);
            return kecamatans;
        }

        static List<Kelurahan> GetListOfKelurahan(Kecamatan kecamatan, string province = "")
        {
            WebClient client = new WebClient();
            string url = GetListOfKelurahanUrl.Replace("{{namakecamatan}}", kecamatan.namaKecamatan);
            url = url.Replace("{{namapropinsi}}", kecamatan.namaPropinsi);
            url = url.Replace("{{namakabupaten}}", kecamatan.namaKabKota);
            string s = client.DownloadString(url);

            RootData rootData = JsonConvert.DeserializeObject<RootData>(s);
            s = JsonConvert.SerializeObject(rootData.aaData);

            List<Kelurahan> kelurahans = JsonConvert.DeserializeObject<List<Kelurahan>>(s);
            return kelurahans;
        }

        static List<Tps> GetListOfTps(Kelurahan kelurahan, string province = "")
        {
            WebClient client = new WebClient();
            string url = GetListOfTpsUrl.Replace("{{namakelurahan}}", kelurahan.namaKelurahan);
            url = url.Replace("{{namapropinsi}}", kelurahan.namaPropinsi);
            url = url.Replace("{{namakabupaten}}", kelurahan.namaKabKota);
            url = url.Replace("{{namakecamatan}}", kelurahan.namaKecamatan);
            string s = client.DownloadString(url);

            RootData rootData = JsonConvert.DeserializeObject<RootData>(s);
            s = JsonConvert.SerializeObject(rootData.aaData);

            List<Tps> tps = JsonConvert.DeserializeObject<List<Tps>>(s);
            return tps;
        }

        static List<Voter> GetListOfVoters(Tps tps, string province = "")
        {

            WebClient client = new WebClient();
            string url = GetListOfPemilihUrl.Replace("{{tps}}", tps.tps);
            url = url.Replace("{{namapropinsi}}", tps.namaPropinsi);
            url = url.Replace("{{namakabupaten}}", tps.namaKabKota);
            url = url.Replace("{{namakecamatan}}", tps.namaKecamatan);
            url = url.Replace("{{namakelurahan}}", tps.namaKelurahan);

            string s = "";

            s = client.DownloadString(url);
           
            RootData rootData = JsonConvert.DeserializeObject<RootData>(s);
            s = JsonConvert.SerializeObject(rootData.data);

            List<Voter> voters = JsonConvert.DeserializeObject<List<Voter>>(s);
            if(province.Length > 0)
            {
                foreach(Voter voter in voters)
                {
                    voter.birthProvince = province;
                }
            }
            return voters;
        }

    }
}
