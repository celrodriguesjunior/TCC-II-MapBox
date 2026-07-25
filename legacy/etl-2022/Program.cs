using System;
using System.Collections.Generic;
using System.IO;

namespace ConversorDeTxtParaSQL
{
    class Program
    {
        static void Main(string[] args)
        {
            //ConverterDadosTrafego();
            //ConverterDadosClima();
            ConverterDadosParaJs();
        }

        public static void ConverterDadosClima()
        {
            var contador = 0;
            var contadorArquivo = 0;
            var arquivos = CarregarListaArquivosClima();
            StreamWriter sw = new StreamWriter("~\\data_exported_clima" + contadorArquivo + ".sql");
            sw.WriteLine("USE [TCC]");
            sw.WriteLine("DELETE FROM DADOS_CLIMA;");
            foreach (var a in arquivos)
            {
                StreamReader sr = new StreamReader(a);

                while (!sr.EndOfStream)
                {
                    var linha = sr.ReadLine();
                    if (linha.IndexOf(";;") == -1)
                    {

                        var partes = linha.Split(';');
                        var insert = "INSERT INTO DADOS_CLIMA VALUES (";

                        for (var i = 0; i < partes.Length; i++)
                        {
                            if (i == 0)
                            {
                                var data = partes[i].Trim();
                                var partesData = data.Split(' ');
                                var date = partesData[0];
                                var hora = partesData[1];
                                var partesDate = date.Split('/');
                                var partesHora = hora.Split(':');
                                var dataEvento = new DateTime(Convert.ToInt32(partesDate[2]), Convert.ToInt32(partesDate[1]), Convert.ToInt32(partesDate[0]),
                                                        Convert.ToInt32(partesHora[0]), Convert.ToInt32(partesHora[1].ToCharArray()[0] + "0"), 00);

                                insert += "'" + dataEvento.ToString("dd/MM/yyyy HH:mm:ss") + "'";

                                //insert += "'" + partes[i] + "'";

                                var latLong = GetCoordenadasClima(a);
                                var lat = latLong.Split(',')[0].Trim();
                                var longi = latLong.Split(',')[1].Trim();

                                insert += ",'" + lat + "'";
                                insert += ",'" + longi + "'";
                            }
                            else
                            {
                                if (i > 2)
                                {
                                    var partes2 = partes[i].Split(':');
                                    if (partes2.Length == 2)
                                    {

                                        var indexString = new List<int> { 4, 5, 6, 7, 22, 27 };
                                        var indexDecimal = new List<int> { 8, 9, 10, 11, 15, 17 };
                                        var indexInt = new List<int> { 3, 12, 13, 14, 16, 18, 19, 20, 21, 23, 24, 25, 26, 28 };

                                        if (indexString.Contains(i))
                                        {
                                            insert += ",'" + partes2[1] + "'";
                                        }
                                        else if (indexDecimal.Contains(i))
                                        {
                                            insert += "," + Convert.ToDecimal(partes2[1]).ToString().Replace(',', '.') + "";
                                        }
                                        else if (indexInt.Contains(i))
                                        {
                                            insert += "," + Convert.ToInt32(partes2[1]) + "";
                                        }
                                    }
                                }
                            }
                        }
                        insert += ")";
                        sw.WriteLine(insert);
                        contador++;
                        if (contador >= 30000)
                        {
                            sw.Close();
                            contadorArquivo++;
                            sw = new StreamWriter("~\\data_exported_clima" + contadorArquivo + ".sql");
                            contador = 0;
                        }
                    }
                }
                sr.Close();
            }
            sw.Close();
        }

        public static void ConverterDadosTrafego()
        {
            var contador = 0;
            var contadorArquivo = 0;
            var arquivos = CarregarListaArquivos();
            StreamWriter sw = new StreamWriter("~\\data_exported"+contadorArquivo+".sql");
            sw.WriteLine("USE [TCC]");
            sw.WriteLine("DELETE FROM DADOS_TRAFEGO;");
            foreach (var a in arquivos)
            {
                StreamReader sr = new StreamReader(a);

                while (!sr.EndOfStream)
                {
                    var linha = sr.ReadLine();
                    if (linha.IndexOf("frc") != -1)
                    {
                        //19/04/2022 00:40:51 -  lat:-26.907517012564714, long:-49.03243355144377;
                        //currentSpeed:44;
                        //freeFlowSpeed:44;
                        //currentTravelTime:473;
                        //freeFlowTravelTime:473;
                        //frc:FRC3;
                        var insert = "INSERT INTO DADOS_TRAFEGO VALUES (";
                        var partes = linha.Split(';');
                        if (partes.Length == 7)
                        {
                            for (var i = 0; i < partes.Length; i++)
                            {
                                if (i == 0)
                                {
                                    var index = partes[i].IndexOf('-');
                                    var partes2 = new string[] { partes[i].Substring(0, index), partes[i].Substring(index + 1, partes[i].Length - (index + 1)) };
                                    var data = partes2[0].Trim();
                                    var partesData = data.Split(' ');
                                    var date = partesData[0];
                                    var hora = partesData[1];
                                    var partesDate = date.Split('/');
                                    var partesHora = hora.Split(':');
                                    var dataEvento = new DateTime(Convert.ToInt32(partesDate[2]), Convert.ToInt32(partesDate[1]), Convert.ToInt32(partesDate[0]),
                                                            Convert.ToInt32(partesHora[0]), Convert.ToInt32(partesHora[1].ToCharArray()[0] + "0"), 00);

                                    insert += "'" + dataEvento.ToString("dd/MM/yyyy HH:mm:ss") + "'";

                                    var latLong = GetCoordenadas(a);
                                    var lat = latLong.Split(',')[0].Trim();
                                    var longi = latLong.Split(',')[1].Trim();

                                    insert += ",'" + lat + "'";
                                    insert += ",'" + longi + "'";

                                }
                                else
                                {
                                    var texto = partes[i].Split(':');
                                    if (texto.Length == 2)
                                    {
                                        if (texto[0] == "frc")
                                        {
                                            insert += ",'" + texto[1] + "')";
                                        }
                                        else
                                        {
                                            insert += "," + texto[1] + "";
                                        }
                                    }
                                }
                            }
                            sw.WriteLine(insert);
                            contador++;
                            if(contador >= 30000)
                            {
                                sw.Close();
                                contadorArquivo++;
                                sw = new StreamWriter("~\\data_exported" + contadorArquivo + ".sql");
                                contador = 0;
                            }
                        }
                    }
                }

                sr.Close();
            }
            sw.Close();
        }

        public static List<string> CarregarListaArquivos()
        {
            var listaArquivos = new List<string>();
            listaArquivos.Add("~\\dados_RuaItajai.txt");
            listaArquivos.Add("~\\dados_AvPresCasteloBranco.txt");
            listaArquivos.Add("~\\dados_Rua7DeSetembro.txt");
            listaArquivos.Add("~\\dados_RuaNereuRamos.txt");
            listaArquivos.Add("~\\dados_RuaFlorianoPeixoto.txt");
            listaArquivos.Add("~\\dados_RuaAmazonas.txt");
            listaArquivos.Add("~\\dados_RuaAntonioDaVeiga.txt");
            listaArquivos.Add("~\\dados_RuaEngenheiroPaulWerner.txt");
            listaArquivos.Add("~\\dados_RuaRepúblicaArgentina.txt");
            listaArquivos.Add("~\\dados_RuaCurtHering.txt");
            listaArquivos.Add("~\\dados_RuaDoutorAmadeuDaLuz.txt");
            listaArquivos.Add("~\\dados_RuaTheodoroHoltrup.txt");
            listaArquivos.Add("~\\dados_RuaPaulHeining.txt");
            listaArquivos.Add("~\\dados_RuaPadreJacobs.txt");
            listaArquivos.Add("~\\dados_RuaCaetanoDeeke.txt");
            listaArquivos.Add("~\\dados_RuaXVDeNovembro.txt");
            listaArquivos.Add("~\\dados_RuaAngeloDias.txt");
            listaArquivos.Add("~\\dados_RuaDrLuizDeFreitasMeiro.txt");
            listaArquivos.Add("~\\dados_RuaEngRodolfoFerraz.txt");
            listaArquivos.Add("~\\dados_RuaCelVidalRamos.txt");
            listaArquivos.Add("~\\dados_RuaAlameidaRioBranco.txt");
            listaArquivos.Add("~\\dados_RuaPresJohnKenedy.txt");
            listaArquivos.Add("~\\dados_RuaNamyDeeke.txt");
            listaArquivos.Add("~\\dados_RuaPresGetulioVargas.txt");
            listaArquivos.Add("~\\dados_RuaMartinLuther.txt");
            listaArquivos.Add("~\\dados_RuaSaoPaulo.txt");
            listaArquivos.Add("~\\dados_RuaPauloZimmermann.txt");
            listaArquivos.Add("~\\dados_PteAPDeAndrade.txt");
            listaArquivos.Add("~\\dados_AvBrasil.txt");
            listaArquivos.Add("~\\dados_RuaParaiba.txt");
            listaArquivos.Add("~\\dados_RuaMaxHering.txt");
            listaArquivos.Add("~\\dados_RuaHeinrichHosang.txt");
            listaArquivos.Add("~\\dados_RuaHumbertoCampos.txt");
            return listaArquivos;
        }

        public static List<string> CarregarListaArquivosClima()
        {
            var listaArquivos = new List<string>();
            listaArquivos.Add("~\\Clima\\dados_RuaItajai_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_AvPresCasteloBranco_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_Rua7DeSetembro_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaNereuRamos_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaFlorianoPeixoto_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaAmazonas_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaAntonioDaVeiga_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaEngenheiroPaulWerner_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaRepúblicaArgentina_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaCurtHering_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaDoutorAmadeuDaLuz_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaTheodoroHoltrup_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaPaulHeining_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaPadreJacobs_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaCaetanoDeeke_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaXVDeNovembro_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaAngeloDias_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaDrLuizDeFreitasMeiro_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaEngRodolfoFerraz_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaCelVidalRamos_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaAlameidaRioBranco_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaPresJohnKenedy_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaNamyDeeke_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaPresGetulioVargas_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaMartinLuther_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaSaoPaulo_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaPauloZimmermann_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_PteAPDeAndrade_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_AvBrasil_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaParaiba_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaMaxHering_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaHeinrichHosang_clima.txt");
            listaArquivos.Add("~\\Clima\\dados_RuaHumbertoCampos_clima.txt");
            return listaArquivos;
        }

        protected static string GetCoordenadas(string arquivo)
        {

            switch (arquivo)
            {
                case "~\\dados_RuaItajai.txt": return "-26.916563715868133, -49.05792660840415";
                case "~\\dados_AvPresCasteloBranco.txt": return "-26.918770816760464, -49.065900446513716";
                case "~\\dados_Rua7DeSetembro.txt": return "-26.919775279947977, -49.068797232177275";
                case "~\\dados_RuaNereuRamos.txt": return "-26.924070455034585, -49.062102438551165";
                case "~\\dados_RuaFlorianoPeixoto.txt": return "-26.92380262503567, -49.06444563508514";
                case "~\\dados_RuaAmazonas.txt": return "-26.92881950574518, -49.05825539761542";
                case "~\\dados_RuaAntonioDaVeiga.txt": return "-26.906712065039613, -49.078479140194204";
                case "~\\dados_RuaEngenheiroPaulWerner.txt": return "-26.896680649078398, -49.08196042611838";
                case "~\\dados_RuaRepúblicaArgentina.txt": return "-26.916581346175274, -49.06329074155425";
                case "~\\dados_RuaCurtHering.txt": return "-26.92103650395346, -49.06555233963728";
                case "~\\dados_RuaDoutorAmadeuDaLuz.txt": return "-26.916134440629143, -49.0708721661667";
                case "~\\dados_RuaTheodoroHoltrup.txt": return "-26.90604508850689, -49.08643025392805";

                case "~\\dados_RuaPaulHeining.txt": return "-26.92145108543909, -49.06556016340889";
                case "~\\dados_RuaPadreJacobs.txt": return "-26.92061643737866, -49.066705466660565";
                case "~\\dados_RuaCaetanoDeeke.txt": return "-26.921912278511677, -49.064661584200806";
                case "~\\dados_RuaXVDeNovembro.txt": return "-26.921244548106717, -49.0632963788271";
                case "~\\dados_RuaAngeloDias.txt": return "-26.923670510818784, -49.06330957626495";

                case "~\\dados_RuaDrLuizDeFreitasMeiro.txt": return "-26.924402303103776, -49.0630762240737";
                case "~\\dados_RuaEngRodolfoFerraz.txt": return "-26.92487103030827, -49.06383528924871";
                case "~\\dados_RuaCelVidalRamos.txt": return "-26.925645860800987, -49.06381383157149";
                case "~\\dados_RuaAlameidaRioBranco.txt": return "-26.925528679992738, -49.061327423793216";
                case "~\\dados_RuaPresJohnKenedy.txt": return "-26.91889213187762, -49.06808722132246";
                case "~\\dados_RuaNamyDeeke.txt": return "-26.91818661147699, -49.06880068891983";
                case "~\\dados_RuaPresGetulioVargas.txt": return "-26.91682578170627, -49.06941759699835";
                case "~\\dados_RuaMartinLuther.txt": return "-26.911637318562782, -49.07037558496595";
                case "~\\dados_RuaSaoPaulo.txt": return "-26.911068082804167, -49.07217802944494";
                case "~\\dados_RuaPauloZimmermann.txt": return "-26.915416205822556, -49.07134654463699";
                case "~\\dados_PteAPDeAndrade.txt": return "-26.912750304372963, -49.06847021580914";
                case "~\\dados_AvBrasil.txt": return "-26.91378976213333, -49.066452071896286";
                case "~\\dados_RuaParaiba.txt": return "-26.913182269867395, -49.07469986467684";
                case "~\\dados_RuaMaxHering.txt": return "-26.910154457105197, -49.075693978358345";
                case "~\\dados_RuaHeinrichHosang.txt": return "-26.91037449996252, -49.079438342168366";
                case "~\\dados_RuaHumbertoCampos.txt": return "-26.913048464465316, -49.08141781243552";
            }

            //-26.916563715868133, -49.05792660840415 - Rua Itajai (hospital santo antonio)
            //-26.918770816760464, -49.065900446513716 - Av. Pres. Castelo Branco - Centro
            //-26.919775279947977, -49.068797232177275 - Rua 7 de setembro - Centro
            //-26.924070455034585, -49.062102438551165 - Rua Nereu Ramos - Centro (escola barão do rio branco)
            //-26.92380262503567, -49.06444563508514 - Rua Floriano Peixoto - Centro (próximo do santa isabel)
            //-26.92881950574518, -49.05825539761542 - Rua Amazonas (proximo hospital santa catarina)
            //-26.906712065039613, -49.078479140194204 - Rua Antonio da Veiga (prox FURB)
            //-26.896680649078398, -49.08196042611838 - Rua Engenheiro Paul Werner (prox FURB campus 3)
            //-26.916581346175274, -49.06329074155425 - Rua República Argentina (centro)
            //-26.92103650395346, -49.06555233963728 - Rua Curt Hering (centro)
            //-26.916134440629143, -49.0708721661667 - Rua Doutor Amadeu da Luz (centro)
            //-26.90604508850689, -49.08643025392805 - Rua Theodoro Holtrup - Vila Nova
            //----
            //-26.92145108543909, -49.06556016340889 - Rua Paul Heining(centro)
            //-26.92061643737866, -49.066705466660565 - Rua Padre Jacobs(centro)
            //-26.921912278511677, -49.064661584200806 - Rua Caetano Deeke(centro)
            //-26.921244548106717, -49.0632963788271 - Rua XV de Novembro(centro)
            //-26.923670510818784, -49.06330957626495 - Rua Ângelo Dias(centro)
            //
            //----
            //
            //-26.924402303103776, -49.0630762240737 - Rua Dr.Luiz de Freitas Meiro(centro)
            //-26.92487103030827, -49.06383528924871 - Rua Eng.Rodolfo Ferraz(centro)
            //-26.925645860800987, -49.06381383157149 - Rua Cel.Vidal Ramos(centro)
            //-26.925528679992738, -49.061327423793216 - Rua Alameida Rio Branco(centro)
            //-26.91889213187762, -49.06808722132246 - Rua Pres.John Kenedy(centro)
            //-26.91818661147699, -49.06880068891983 - Rua Namy Deeke(centro)
            //-26.91682578170627, -49.06941759699835 - Rua Pres.Getúlio Vargas(centro)
            //-26.911637318562782, -49.07037558496595 - Rua Martin Luther(centro)
            //-26.911068082804167, -49.07217802944494 - Rua São Paulo(centro)
            //-26.915416205822556, -49.07134654463699 - Rua Paulo Zimmermann(centro)
            //-26.912750304372963, -49.06847021580914 - Pte.A P de Andrade(centro)
            //-26.91378976213333, -49.066452071896286 - Av.Brasil(centro)
            //-26.913182269867395, -49.07469986467684 - Rua Paraíba(centro)
            //-26.910154457105197, -49.075693978358345 - Rua Max Hering(centro)
            //-26.91037449996252, -49.079438342168366 - Rua Heinrich Hosang(centro)
            //-26.913048464465316, -49.08141781243552 - Rua Humberto Campos(centro)


            return "";
        }

        protected static string GetCoordenadasClima(string arquivo)
        {

            switch (arquivo)
            {
                case "~\\Clima\\dados_RuaItajai_clima.txt": return "-26.916563715868133, -49.05792660840415";
                case "~\\Clima\\dados_AvPresCasteloBranco_clima.txt": return "-26.918770816760464, -49.065900446513716";
                case "~\\Clima\\dados_Rua7DeSetembro_clima.txt": return "-26.919775279947977, -49.068797232177275";
                case "~\\Clima\\dados_RuaNereuRamos_clima.txt": return "-26.924070455034585, -49.062102438551165";
                case "~\\Clima\\dados_RuaFlorianoPeixoto_clima.txt": return "-26.92380262503567, -49.06444563508514";
                case "~\\Clima\\dados_RuaAmazonas_clima.txt": return "-26.92881950574518, -49.05825539761542";
                case "~\\Clima\\dados_RuaAntonioDaVeiga_clima.txt": return "-26.906712065039613, -49.078479140194204";
                case "~\\Clima\\dados_RuaEngenheiroPaulWerner_clima.txt": return "-26.896680649078398, -49.08196042611838";
                case "~\\Clima\\dados_RuaRepúblicaArgentina_clima.txt": return "-26.916581346175274, -49.06329074155425";
                case "~\\Clima\\dados_RuaCurtHering_clima.txt": return "-26.92103650395346, -49.06555233963728";
                case "~\\Clima\\dados_RuaDoutorAmadeuDaLuz_clima.txt": return "-26.916134440629143, -49.0708721661667";
                case "~\\Clima\\dados_RuaTheodoroHoltrup_clima.txt": return "-26.90604508850689, -49.08643025392805";

                case "~\\Clima\\dados_RuaPaulHeining_clima.txt": return "-26.92145108543909, -49.06556016340889";
                case "~\\Clima\\dados_RuaPadreJacobs_clima.txt": return "-26.92061643737866, -49.066705466660565";
                case "~\\Clima\\dados_RuaCaetanoDeeke_clima.txt": return "-26.921912278511677, -49.064661584200806";
                case "~\\Clima\\dados_RuaXVDeNovembro_clima.txt": return "-26.921244548106717, -49.0632963788271";
                case "~\\Clima\\dados_RuaAngeloDias_clima.txt": return "-26.923670510818784, -49.06330957626495";

                case "~\\Clima\\dados_RuaDrLuizDeFreitasMeiro_clima.txt": return "-26.924402303103776, -49.0630762240737";
                case "~\\Clima\\dados_RuaEngRodolfoFerraz_clima.txt": return "-26.92487103030827, -49.06383528924871";
                case "~\\Clima\\dados_RuaCelVidalRamos_clima.txt": return "-26.925645860800987, -49.06381383157149";
                case "~\\Clima\\dados_RuaAlameidaRioBranco_clima.txt": return "-26.925528679992738, -49.061327423793216";
                case "~\\Clima\\dados_RuaPresJohnKenedy_clima.txt": return "-26.91889213187762, -49.06808722132246";
                case "~\\Clima\\dados_RuaNamyDeeke_clima.txt": return "-26.91818661147699, -49.06880068891983";
                case "~\\Clima\\dados_RuaPresGetulioVargas_clima.txt": return "-26.91682578170627, -49.06941759699835";
                case "~\\Clima\\dados_RuaMartinLuther_clima.txt": return "-26.911637318562782, -49.07037558496595";
                case "~\\Clima\\dados_RuaSaoPaulo_clima.txt": return "-26.911068082804167, -49.07217802944494";
                case "~\\Clima\\dados_RuaPauloZimmermann_clima.txt": return "-26.915416205822556, -49.07134654463699";
                case "~\\Clima\\dados_PteAPDeAndrade_clima.txt": return "-26.912750304372963, -49.06847021580914";
                case "~\\Clima\\dados_AvBrasil_clima.txt": return "-26.91378976213333, -49.066452071896286";
                case "~\\Clima\\dados_RuaParaiba_clima.txt": return "-26.913182269867395, -49.07469986467684";
                case "~\\Clima\\dados_RuaMaxHering_clima.txt": return "-26.910154457105197, -49.075693978358345";
                case "~\\Clima\\dados_RuaHeinrichHosang_clima.txt": return "-26.91037449996252, -49.079438342168366";
                case "~\\Clima\\dados_RuaHumbertoCampos_clima.txt": return "-26.913048464465316, -49.08141781243552";
            }

            //-26.916563715868133, -49.05792660840415 - Rua Itajai (hospital santo antonio)
            //-26.918770816760464, -49.065900446513716 - Av. Pres. Castelo Branco - Centro
            //-26.919775279947977, -49.068797232177275 - Rua 7 de setembro - Centro
            //-26.924070455034585, -49.062102438551165 - Rua Nereu Ramos - Centro (escola barão do rio branco)
            //-26.92380262503567, -49.06444563508514 - Rua Floriano Peixoto - Centro (próximo do santa isabel)
            //-26.92881950574518, -49.05825539761542 - Rua Amazonas (proximo hospital santa catarina)
            //-26.906712065039613, -49.078479140194204 - Rua Antonio da Veiga (prox FURB)
            //-26.896680649078398, -49.08196042611838 - Rua Engenheiro Paul Werner (prox FURB campus 3)
            //-26.916581346175274, -49.06329074155425 - Rua República Argentina (centro)
            //-26.92103650395346, -49.06555233963728 - Rua Curt Hering (centro)
            //-26.916134440629143, -49.0708721661667 - Rua Doutor Amadeu da Luz (centro)
            //-26.90604508850689, -49.08643025392805 - Rua Theodoro Holtrup - Vila Nova
            //----
            //-26.92145108543909, -49.06556016340889 - Rua Paul Heining(centro)
            //-26.92061643737866, -49.066705466660565 - Rua Padre Jacobs(centro)
            //-26.921912278511677, -49.064661584200806 - Rua Caetano Deeke(centro)
            //-26.921244548106717, -49.0632963788271 - Rua XV de Novembro(centro)
            //-26.923670510818784, -49.06330957626495 - Rua Ângelo Dias(centro)
            //
            //----
            //
            //-26.924402303103776, -49.0630762240737 - Rua Dr.Luiz de Freitas Meiro(centro)
            //-26.92487103030827, -49.06383528924871 - Rua Eng.Rodolfo Ferraz(centro)
            //-26.925645860800987, -49.06381383157149 - Rua Cel.Vidal Ramos(centro)
            //-26.925528679992738, -49.061327423793216 - Rua Alameida Rio Branco(centro)
            //-26.91889213187762, -49.06808722132246 - Rua Pres.John Kenedy(centro)
            //-26.91818661147699, -49.06880068891983 - Rua Namy Deeke(centro)
            //-26.91682578170627, -49.06941759699835 - Rua Pres.Getúlio Vargas(centro)
            //-26.911637318562782, -49.07037558496595 - Rua Martin Luther(centro)
            //-26.911068082804167, -49.07217802944494 - Rua São Paulo(centro)
            //-26.915416205822556, -49.07134654463699 - Rua Paulo Zimmermann(centro)
            //-26.912750304372963, -49.06847021580914 - Pte.A P de Andrade(centro)
            //-26.91378976213333, -49.066452071896286 - Av.Brasil(centro)
            //-26.913182269867395, -49.07469986467684 - Rua Paraíba(centro)
            //-26.910154457105197, -49.075693978358345 - Rua Max Hering(centro)
            //-26.91037449996252, -49.079438342168366 - Rua Heinrich Hosang(centro)
            //-26.913048464465316, -49.08141781243552 - Rua Humberto Campos(centro)


            return "";
        }

        protected static void ConverterDadosParaJs()
        {
            var arquivo = "~\\dadoscsv2.csv";

            var arquivosGerados = GetArquivosParaGerar();

            StreamReader sr = new StreamReader(arquivo);
            bool jaLeuHeader = false;
            while (!sr.EndOfStream)
            {

                var linha = sr.ReadLine();
                if (jaLeuHeader)
                {
                    var partes = linha.Split(";");
                    var nro = Convert.ToInt32(partes[0]);
                    arquivosGerados[nro - 1].WriteLine("    retorno.push({data_hora:'" + partes[1] + "',current:" + partes[2] + ",free:" + partes[3] +
                        ",proporcao:" + partes[4] + ",clima:" + (partes[5] == "NULL" ? "undefined" : partes[5]) +
                        ",dia_semana:" + partes[6] + ",hora:" + partes[7] + ",turno:" + partes[8] + ",periodo:" + partes[9] + ", minuto: " + partes[10] + "});");
                }
                else
                {
                    jaLeuHeader = true;
                }

            }
            FecharArquivosJs(arquivosGerados);
        }

        protected static List<StreamWriter> GetArquivosParaGerar()
        {
            var retorno = new List<StreamWriter>();
            for (var i = 1; i <= 33; i++)
            {
                var sw = new StreamWriter("~\\js\\dados" + i + ".js");
                sw.WriteLine("function GetDados" + i + "(){");
                sw.WriteLine("  var retorno = [];");

                retorno.Add(sw);
            }

            return retorno;
        }

        protected static void FecharArquivosJs(List<StreamWriter> arquivosGerados)
        {
            foreach (StreamWriter sw in arquivosGerados)
            {
                sw.WriteLine("  return retorno;");
                sw.WriteLine("}");
                sw.Close();
            }
        }

    }

}
