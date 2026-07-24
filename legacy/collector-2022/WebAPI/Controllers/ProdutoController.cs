using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class ProdutoController : ApiController
    {
        public Produto[] produtos = new Produto[] {
            new Produto() { Codigo = 1, Nome = "Nokia Lumia", Categoria = "Celulares", Valor = 699.99M },
            new Produto() { Codigo = 2, Nome = "Samsung Galaxy", Categoria = "Celulares", Valor = 1199.99M },
            new Produto() { Codigo = 3, Nome = "Teclado", Categoria = "Informatica", Valor = 130.00M },
            new Produto() { Codigo = 4, Nome = "Mouse", Categoria = "Informatica", Valor = 59.95M },
            new Produto() { Codigo = 5, Nome = "Case GoPro", Categoria = "Acessorios", Valor = 65.00M },
        };

        //GET = Selects


        public IEnumerable<Produto> GetTodos()
        {
            return produtos;
        }

        public IHttpActionResult Get(int codigo)
        {
            var produto = produtos.FirstOrDefault(p => p.Codigo == codigo);
            if (produto == null) return NotFound();
            return Ok(produto);
            //return Json<Produto>(produto);
        }

        public IEnumerable<Produto> GetPorCategoria(string categoria)
        {
            return produtos.Where(p => string.Equals(p.Categoria, categoria, StringComparison.OrdinalIgnoreCase));
        }

        //POST = Insert
        public void Post([FromBody] ObjetoAPI produto)
        {
            if (produto.flowSegmentData != null)
                Escrever.escreverDados(produto.flowSegmentData);
            else if (produto.weatherData != null)
                Escrever.escreverDadosClima(produto.weatherData);
        }

        //PUT = Insert ou Update
        public void Put([FromBody] WheaterData produto)
        {
            var teste = 0;
        }

        //DELETE = Delete
        public void Delete(int codigo)
        {
        }

    }

    public class ObjetoAPI
    {
        public flowSegmentData flowSegmentData { get; set; }
        public WheaterData weatherData { get; set; }
    }

    public static class Escrever
    {
        public static void escreverDados(flowSegmentData dados)
        {
            StreamWriter sw;

            switch (dados.nroRua)
            {
                case (int)NomeRua.RuaItajai:
                    sw = new StreamWriter("~\\dados_RuaItajai.txt", true);
                    break;
                case (int)NomeRua.AvPresCasteloBranco:
                    sw = new StreamWriter("~\\dados_AvPresCasteloBranco.txt", true);
                    break;
                case (int)NomeRua.Rua7DeSetembro:
                    sw = new StreamWriter("~\\dados_Rua7DeSetembro.txt", true);
                    break;
                case (int)NomeRua.RuaNereuRamos:
                    sw = new StreamWriter("~\\dados_RuaNereuRamos.txt", true);
                    break;
                case (int)NomeRua.RuaFlorianoPeixoto:
                    sw = new StreamWriter("~\\dados_RuaFlorianoPeixoto.txt", true);
                    break;
                case (int)NomeRua.RuaAmazonas:
                    sw = new StreamWriter("~\\dados_RuaAmazonas.txt", true);
                    break;
                case (int)NomeRua.RuaAntonioDaVeiga:
                    sw = new StreamWriter("~\\dados_RuaAntonioDaVeiga.txt", true);
                    break;
                case (int)NomeRua.RuaEngenheiroPaulWerner:
                    sw = new StreamWriter("~\\dados_RuaEngenheiroPaulWerner.txt", true);
                    break;
                case (int)NomeRua.RuaRepúblicaArgentina:
                    sw = new StreamWriter("~\\dados_RuaRepúblicaArgentina.txt", true);
                    break;
                case (int)NomeRua.RuaCurtHering:
                    sw = new StreamWriter("~\\dados_RuaCurtHering.txt", true);
                    break;
                case (int)NomeRua.RuaDoutorAmadeuDaLuz:
                    sw = new StreamWriter("~\\dados_RuaDoutorAmadeuDaLuz.txt", true);
                    break;
                case (int)NomeRua.RuaTheodoroHoltrup:
                    sw = new StreamWriter("~\\dados_RuaTheodoroHoltrup.txt", true);
                    break;
                case (int)NomeRua.RuaPaulHeining:
                    sw = new StreamWriter("~\\dados_RuaPaulHeining.txt", true);
                    break;
                case (int)NomeRua.RuaPadreJacobs:
                    sw = new StreamWriter("~\\dados_RuaPadreJacobs.txt", true);
                    break;
                case (int)NomeRua.RuaCaetanoDeeke:
                    sw = new StreamWriter("~\\dados_RuaCaetanoDeeke.txt", true);
                    break;
                case (int)NomeRua.RuaXVDeNovembro:
                    sw = new StreamWriter("~\\dados_RuaXVDeNovembro.txt", true);
                    break;
                case (int)NomeRua.RuaAngeloDias:
                    sw = new StreamWriter("~\\dados_RuaAngeloDias.txt", true);
                    break;
                case (int)NomeRua.RuaDrLuizDeFreitasMeiro:
                    sw = new StreamWriter("~\\dados_RuaDrLuizDeFreitasMeiro.txt", true);
                    break;
                case (int)NomeRua.RuaEngRodolfoFerraz:
                    sw = new StreamWriter("~\\dados_RuaEngRodolfoFerraz.txt", true);
                    break;
                case (int)NomeRua.RuaCelVidalRamos:
                    sw = new StreamWriter("~\\dados_RuaCelVidalRamos.txt", true);
                    break;
                case (int)NomeRua.RuaAlameidaRioBranco:
                    sw = new StreamWriter("~\\dados_RuaAlameidaRioBranco.txt", true);
                    break;
                case (int)NomeRua.RuaPresJohnKenedy:
                    sw = new StreamWriter("~\\dados_RuaPresJohnKenedy.txt", true);
                    break;
                case (int)NomeRua.RuaNamyDeeke:
                    sw = new StreamWriter("~\\dados_RuaNamyDeeke.txt", true);
                    break;
                case (int)NomeRua.RuaPresGetulioVargas:
                    sw = new StreamWriter("~\\dados_RuaPresGetulioVargas.txt", true);
                    break;
                case (int)NomeRua.RuaMartinLuther:
                    sw = new StreamWriter("~\\dados_RuaMartinLuther.txt", true);
                    break;
                case (int)NomeRua.RuaSaoPaulo:
                    sw = new StreamWriter("~\\dados_RuaSaoPaulo.txt", true);
                    break;
                case (int)NomeRua.RuaPauloZimmermann:
                    sw = new StreamWriter("~\\dados_RuaPauloZimmermann.txt", true);
                    break;
                case (int)NomeRua.PteAPDeAndrade:
                    sw = new StreamWriter("~\\dados_PteAPDeAndrade.txt", true);
                    break;
                case (int)NomeRua.AvBrasil:
                    sw = new StreamWriter("~\\dados_AvBrasil.txt", true);
                    break;
                case (int)NomeRua.RuaParaiba:
                    sw = new StreamWriter("~\\dados_RuaParaiba.txt", true);
                    break;
                case (int)NomeRua.RuaMaxHering:
                    sw = new StreamWriter("~\\dados_RuaMaxHering.txt", true);
                    break;
                case (int)NomeRua.RuaHeinrichHosang:
                    sw = new StreamWriter("~\\dados_RuaHeinrichHosang.txt", true);
                    break;
                case (int)NomeRua.RuaHumbertoCampos:
                    sw = new StreamWriter("~\\dados_RuaHumbertoCampos.txt", true);
                    break;
                default:
                    sw = new StreamWriter("~\\dados_default.txt", true);
                    break;

            }

            sw.WriteLine(DateTime.Now + " - " + dados.ToString() + ";");

            sw.Close();
        }

        public static void escreverDadosClima(WheaterData dados)
        {
            StreamWriter sw;

            switch (dados.nroRua)
            {
                case (int)NomeRua.RuaItajai:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaItajai_clima.txt", true);
                    break;
                case (int)NomeRua.AvPresCasteloBranco:
                    sw = new StreamWriter("~\\dados_clima\\dados_AvPresCasteloBranco_clima.txt", true);
                    break;
                case (int)NomeRua.Rua7DeSetembro:
                    sw = new StreamWriter("~\\dados_clima\\dados_Rua7DeSetembro_clima.txt", true);
                    break;
                case (int)NomeRua.RuaNereuRamos:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaNereuRamos_clima.txt", true);
                    break;
                case (int)NomeRua.RuaFlorianoPeixoto:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaFlorianoPeixoto_clima.txt", true);
                    break;
                case (int)NomeRua.RuaAmazonas:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaAmazonas_clima.txt", true);
                    break;
                case (int)NomeRua.RuaAntonioDaVeiga:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaAntonioDaVeiga_clima.txt", true);
                    break;
                case (int)NomeRua.RuaEngenheiroPaulWerner:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaEngenheiroPaulWerner_clima.txt", true);
                    break;
                case (int)NomeRua.RuaRepúblicaArgentina:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaRepúblicaArgentina_clima.txt", true);
                    break;
                case (int)NomeRua.RuaCurtHering:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaCurtHering_clima.txt", true);
                    break;
                case (int)NomeRua.RuaDoutorAmadeuDaLuz:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaDoutorAmadeuDaLuz_clima.txt", true);
                    break;
                case (int)NomeRua.RuaTheodoroHoltrup:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaTheodoroHoltrup_clima.txt", true);
                    break;
                case (int)NomeRua.RuaPaulHeining:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaPaulHeining_clima.txt", true);
                    break;
                case (int)NomeRua.RuaPadreJacobs:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaPadreJacobs_clima.txt", true);
                    break;
                case (int)NomeRua.RuaCaetanoDeeke:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaCaetanoDeeke_clima.txt", true);
                    break;
                case (int)NomeRua.RuaXVDeNovembro:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaXVDeNovembro_clima.txt", true);
                    break;
                case (int)NomeRua.RuaAngeloDias:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaAngeloDias_clima.txt", true);
                    break;
                case (int)NomeRua.RuaDrLuizDeFreitasMeiro:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaDrLuizDeFreitasMeiro_clima.txt", true);
                    break;
                case (int)NomeRua.RuaEngRodolfoFerraz:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaEngRodolfoFerraz_clima.txt", true);
                    break;
                case (int)NomeRua.RuaCelVidalRamos:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaCelVidalRamos_clima.txt", true);
                    break;
                case (int)NomeRua.RuaAlameidaRioBranco:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaAlameidaRioBranco_clima.txt", true);
                    break;
                case (int)NomeRua.RuaPresJohnKenedy:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaPresJohnKenedy_clima.txt", true);
                    break;
                case (int)NomeRua.RuaNamyDeeke:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaNamyDeeke_clima.txt", true);
                    break;
                case (int)NomeRua.RuaPresGetulioVargas:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaPresGetulioVargas_clima.txt", true);
                    break;
                case (int)NomeRua.RuaMartinLuther:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaMartinLuther_clima.txt", true);
                    break;
                case (int)NomeRua.RuaSaoPaulo:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaSaoPaulo_clima.txt", true);
                    break;
                case (int)NomeRua.RuaPauloZimmermann:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaPauloZimmermann_clima.txt", true);
                    break;
                case (int)NomeRua.PteAPDeAndrade:
                    sw = new StreamWriter("~\\dados_clima\\dados_PteAPDeAndrade_clima.txt", true);
                    break;
                case (int)NomeRua.AvBrasil:
                    sw = new StreamWriter("~\\dados_clima\\dados_AvBrasil_clima.txt", true);
                    break;
                case (int)NomeRua.RuaParaiba:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaParaiba_clima.txt", true);
                    break;
                case (int)NomeRua.RuaMaxHering:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaMaxHering_clima.txt", true);
                    break;
                case (int)NomeRua.RuaHeinrichHosang:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaHeinrichHosang_clima.txt", true);
                    break;
                case (int)NomeRua.RuaHumbertoCampos:
                    sw = new StreamWriter("~\\dados_clima\\dados_RuaHumbertoCampos_clima.txt", true);
                    break;
                default:
                    sw = new StreamWriter("~\\dados_clima\\dados_default_clima.txt", true);
                    break;

            }

            sw.WriteLine(DateTime.Now + ";" + dados.ToString() + "");

            sw.Close();
        }

    }


    public enum NomeRua
    {
        RuaItajai = 1,
        AvPresCasteloBranco = 2,
        Rua7DeSetembro = 3,
        RuaNereuRamos = 4,
        RuaFlorianoPeixoto = 5,
        RuaAmazonas = 6,
        RuaAntonioDaVeiga = 7,
        RuaEngenheiroPaulWerner = 8,
        RuaRepúblicaArgentina = 9,
        RuaCurtHering = 10,
        RuaDoutorAmadeuDaLuz = 11,
        RuaTheodoroHoltrup = 12,
        RuaPaulHeining = 13,
        RuaPadreJacobs = 14,
        RuaCaetanoDeeke = 15,
        RuaXVDeNovembro = 16,
        RuaAngeloDias = 17,
        RuaDrLuizDeFreitasMeiro = 18,
        RuaEngRodolfoFerraz = 19,
        RuaCelVidalRamos = 20,
        RuaAlameidaRioBranco = 21,
        RuaPresJohnKenedy = 22,
        RuaNamyDeeke = 23,
        RuaPresGetulioVargas = 24,
        RuaMartinLuther = 25,
        RuaSaoPaulo = 26,
        RuaPauloZimmermann = 27,
        PteAPDeAndrade = 28,
        AvBrasil = 29,
        RuaParaiba = 30,
        RuaMaxHering = 31,
        RuaHeinrichHosang = 32,
        RuaHumbertoCampos = 33
    }

    public class ClimaController : ApiController
    {
        public void Post([FromBody] WheaterData wheater)
        {
            var teste = 0;
            //realiza o cadastro de produto com base no parametro recebido
        }
    }

}
