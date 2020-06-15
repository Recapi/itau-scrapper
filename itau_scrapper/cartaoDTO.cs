using System;
using System.Collections.Generic;
using System.Text;

namespace itau_scrapper
{
    public class cartaoDTO
    {
        public string Nome { get; set; }
        public string Final { get; set; }
        public double Disponivel { get; set; }
        public double FaturaAtual { get; set; }
        public bool Fechada { get; set; }
        public DateTime dataVencimentoProxfatura { get; set; }
        public override string ToString()
        {
            return ("O cartao " + Nome +" de final " + Final + " tem disponivel " +Disponivel + " e com fatura atual de " + FaturaAtual + " " + (Fechada ? " fechada" : "aberta") + " com vencimento " + dataVencimentoProxfatura.ToString("dd/MM/yyyy"));
        }
    }

  
}
