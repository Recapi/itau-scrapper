using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace itau_scrapper
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Entre com a sua agencia 4 digitos : ");
            var agencia = Console.ReadLine();
            agencia = agencia.Length == 4 ? agencia : "SUA_AGENCIA_PARA_NAO_FICAR_DIGITANDO_E_APERTAR_ENTER";
            Console.WriteLine("Entre com a sua conta com digito 6 digitos : ");
            var conta = Console.ReadLine();
            conta = conta.Length == 6 ? conta : "LIKE_AGENCIA";

            Console.WriteLine("Entre com a sua senha eletronica com digito 6 digitos : ");

            var senha = "";
            senha = getSenha(senha);
            senha = senha.Length == 6 ? senha : "LIKE_AGENCIA";


            Console.Clear();



            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");
            chromeOptions.AddArguments("window-size=1920,1080");
            chromeOptions.AddArguments("start-maximized");


            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(chromeDriverService, chromeOptions);
            driver.Manage().Window.Maximize();


            Console.WriteLine("Acessando site do Itau");
            driver.Navigate().GoToUrl("https://www.itau.com.br/");




            WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 2, 0));

            try
            {
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id("agencia")));

            }
            catch (Exception)
            {

                Console.WriteLine("Nao foi possivel acessar o site : Enter para sair");
                driver.Dispose();
                Console.ReadLine();

                Environment.Exit(0);
            }

            Console.WriteLine("Digitando Agencia e Conta");
            driver.FindElement(By.Id("agencia")).SendKeys(agencia + conta);
            Thread.Sleep(1000);

            driver.FindElement(By.Id("btnLoginSubmit")).Click();



            Console.WriteLine("Aguardando Carregar Teclado Virtual");
            WebDriverWait wait2 = new WebDriverWait(driver, new TimeSpan(0, 2, 0));
            try
            {
                wait2.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText(senha[0].ToString())));

            }
            catch
            {
                Console.WriteLine("Impossivel achar o Teclado Virutal : Enter para sair");
                driver.Dispose();
                Console.ReadLine();

                Environment.Exit(0);
            }

            Thread.Sleep(1000);




            Console.WriteLine("Digitando no Teclado Virtual");
            foreach (var item in senha.ToCharArray())
            {
                driver.FindElement(By.PartialLinkText(item.ToString())).Click();
            }

            Console.WriteLine("Acessando Conta");
            driver.FindElement(By.Id("acessar")).Click();

            Console.WriteLine("Esperando Carregar Conta");

            WebDriverWait wait3 = new WebDriverWait(driver, new TimeSpan(0, 2, 0));
            try
            {
                wait3.Until(ExpectedConditions.ElementIsVisible(By.Id("saldo")));

            }
            catch (Exception)
            {

                Console.WriteLine("Não Conseguimos ver o saldo : Enter para sair");
                driver.Dispose();
                Console.ReadLine();

                Environment.Exit(0);
            }

            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("popFechar()");

            var saldoHTML = driver.FindElement(By.Id("saldo")).GetAttribute("innerHTML");
            var primeiro = saldoHTML.IndexOf("<small>");
            var ultimo = saldoHTML.LastIndexOf("</small>");


            var substring = saldoHTML.Substring(primeiro, ultimo - primeiro).Replace("<small>", "").Replace("</small>", "").Replace("R$", "").Replace(@"\r", "").Replace(@"\n", "").Replace(@"\t", "").Trim();

            double saldo = Double.Parse(substring);

            //ACESSA MENU
            //Actions builder = new Actions(driver);
            //IWebElement element = driver.FindElement(By.ClassName("ico-header-menu"));
            //builder.MoveToElement(element).Build().Perform();
            //try
            //{
            //}
            //catch (Exception e)
            //{
            //}


            driver.FindElement(By.Id("accordionExibirBoxCartoes")).Click();

            Thread.Sleep(2000);


            var cartHTML = driver.FindElement(By.Id("exibirBoxCartoes")).GetAttribute("innerHTML");
            var primeirotable = cartHTML.IndexOf("<table");
            var ultimotable = cartHTML.LastIndexOf("</table>");

            var substringCartao = cartHTML.Substring(primeirotable, ultimotable - primeirotable + 9).Replace(@"\r", "").Replace(@"\n", "").Replace(@"\t", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();


            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(substringCartao);

            var HTMLTableTRList = from table in doc.DocumentNode.SelectNodes("//table").Cast<HtmlNode>()
                                  from row in table.SelectNodes("//tr").Cast<HtmlNode>()
                                  from cell in row.SelectNodes("th|td").Cast<HtmlNode>()
                                  select new { Table_Name = table.Id, Cell_Text = cell.InnerText };

            List<cartaoDTO> listaCartao = new List<cartaoDTO>();
            int celula = 1;
            cartaoDTO cartao = new cartaoDTO();
            foreach (var cell in HTMLTableTRList.Skip(4))
            {
                var texto = Regex.Replace(cell.Cell_Text, @"\s+", " ").Trim();

                if (celula ==1 )
                {
                    cartao = new cartaoDTO();
                    var posicaofinal = texto.IndexOf("final");
                    var posicaolimite = texto.IndexOf("Limite disponível");
                    
                    cartao.Nome = texto.Substring(0, posicaofinal-2).Trim();
                    cartao.Final = texto.Substring(posicaofinal+5, posicaolimite - posicaofinal-5).Trim();

                    var textoDisponivel = texto.Substring(posicaolimite+ "Limite disponível".Length, texto.Length - posicaolimite- "Limite disponível".Length).Replace("R$", "").Replace(":", "").Trim();
                    cartao.Disponivel = Double.Parse(textoDisponivel);
                }

                if (celula == 2)
                {
                    int dia = Int32.Parse(texto.Substring(0, 2));
                    int mes = Int32.Parse(texto.Substring(3, 2));
                    int ano = Int32.Parse(texto.Substring(6, 4));

                    cartao.dataVencimentoProxfatura = new DateTime(ano, mes, dia);
                }
                if (celula == 3)
                {
                    cartao.FaturaAtual = Double.Parse(texto);
                }

                if (celula == 4)
                {
                    cartao.Fechada = !texto.Equals("aberta");
                    listaCartao.Add(cartao);
                    celula = 0;

                }

                celula++;

            }

            driver.Dispose();

            Console.WriteLine("Seu saldo : " + saldo);
            Console.WriteLine("Seu(s) Cartões");

            foreach (var item in listaCartao)
            {
                Console.WriteLine(item.ToString());
            }

            
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path + @"\Saldo.txt", true))
            {
                file.WriteLine(saldo + " na data de : " + DateTime.Now);
                foreach (var item in listaCartao)
                {
                    file.WriteLine(item.ToString() + " na data de : " + DateTime.Now);
                }
            }

        }

        static string getSenha(string senha)
        {
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    senha += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && senha.Length > 0)
                    {
                        senha = senha.Substring(0, (senha.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        Console.Write("\n");
                        break;
                    }
                }
            } while (true);

            return senha;
        }
    }
}
