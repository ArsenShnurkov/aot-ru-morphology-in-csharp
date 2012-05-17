using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Morphology
{



public struct MorphInfo
{
    public string word;
    public string normalForm;
    public string pos;
    public string gramem;
}

public struct Record
{
	public string paradigm;
	public string accent;
	public string session;
	public string ancode;
	public string prefix;
}

public class AotDictionary
{
    MrdDictionary mrd;
    GramtabFile gramtab;

    public AotDictionary(string aotPath)
    {
        gramtab = new GramtabFile   (aotPath + @"\Dicts\Morph\rgramtab.tab");
        mrd = new MrdDictionary     (aotPath + @"\Dicts\SrcMorph\RusSrc\morphs.mrd");
    }

    public string[] GetGramem(string word)
    {
        if (word.Length == 0)
            return new string[] {};
        List<Record> records = mrd.GetNolemRecords(word);
        string ending;
        string lemma = word;
        bool ifNolem = false;
        if (records.Count > 0)
        {
            ifNolem = true;
            ending = lemma;
        }
        else
        {
            int cutl = word.Length + 1;
            do
            {
                cutl--;
                lemma = word.Substring(0, cutl);
                records = mrd.GetRecords(lemma);
            } while (records.Count == 0 && cutl > 1);
            if (records.Count == 0)
                return new string[] { };
            ending = word.Substring(cutl, word.Length - lemma.Length);
        }
        // Среди всех парадигм данной леммы ищем
        // наилучшее окончание, возвращаем пару (нормальная_форма, ближайшая_форма).

        double priority = 1;
        var lemmas = new List<Tuple<double, string, string>>();
        foreach(Record r in records)
        {
            priority = 1;
            char[] dd = {'%'};
            string[] flexions = r.paradigm.Split(dd, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            string first_form = "";
            string best_form  = "";
            foreach (string fa in flexions)
            {
                string[] form = fa.Split('*');
                string flexion = form[0];
                string ancode  = form[1];
                if (i == 0)
                {
                    first_form = fa;
                    best_form  = first_form;
                }
                if (ending == flexion)
                {
                    double p = -1 + 0.5 * i / flexions.Length;
                    if ( p < priority )
                    {
                        priority = p;
                        best_form = fa;
                    }
                }
                else if (flexion.StartsWith(ending))
                {
                    double p = 0.5 * i / flexions.Length;
                    if ( p < priority )
                    {
                        priority = p;
                        best_form = fa;
                    }
                }
                ++i;
            }
            lemmas.Add( Tuple.Create(priority, first_form, best_form)  );
        }
        lemmas.Sort();
        var best = lemmas[0];
        string[] norm_f = best.Item2.Split('*');
        string[] best_f = best.Item3.Split('*');
        string bac = best_f[1];
        string bgramem = gramtab.GetGramem(bac);
        string nac = norm_f[1];
        string ngramem = gramtab.GetGramem(nac);
        if (ifNolem)
        {
            lemma = "";
        }
        string normal_word = lemma + norm_f[0];
        string best_word   = lemma + best_f[0];

        string gramem = bgramem;
        string[] result = { String.Format("{0}[{1}][{2}]", normal_word, best_word, gramem) };
        return result;
    }
}

class GramtabFile
{

	public Dictionary<string,string> Ancode2gramem = new Dictionary<string,string>();

	public string GetGramem(string ancode)
    {
		if (Ancode2gramem.ContainsKey(ancode))
			return Ancode2gramem[ancode];
		return "";
	}
	
	public GramtabFile(string tabPath)
    {
        string[] tabLines = new string[0];
        tabLines = System.IO.File.ReadAllLines(tabPath, Encoding.GetEncoding(1251));
		foreach(string line in tabLines) {
			if ( line.StartsWith("//") || line.Trim() == String.Empty )
				continue;
			string[] r = line.Split(' ');
            if (r.Length < 3)
            {
                throw new Exception("Неверный формат gramtab-файла.");
            }
			string ancode	= r[0];
			//string tmp1	= r[1];
			string pos		= r[2];
			string gramems	= "-";
			if( r.Length > 3 ) {
			  gramems	= r[3];
			}
            if (Ancode2gramem.ContainsKey(ancode))
            {
                throw new Exception("Повторное объявление анкода.");
            }
			Ancode2gramem.Add(ancode, pos+" "+gramems);
		}
	}

}

class MrdDictionary
{

	public Dictionary<string,List<Record>> Lemma2records = new Dictionary<string,List<Record>>();
    public Dictionary<string,List<Record>> Nolem2records = new Dictionary<string, List<Record>>();

    public List<Record> GetNolemRecords(string word)
    {
        if (Nolem2records.ContainsKey(word))
            return Nolem2records[word];
        return new List<Record>();
    }

	public List<Record> GetRecords(string lemma)
    {
		if (Lemma2records.ContainsKey(lemma))
			return Lemma2records[lemma];
		return new List<Record>();
	}
	
	public MrdDictionary(string mrdPath)
    {
		/* Загружает весь словарь в память (~5MB). */
		string[] lines = System.IO.File.ReadAllLines(mrdPath, Encoding.GetEncoding(1251));
		int count = 0;
		int paradigms_size = int.Parse(lines[count]);
		count += paradigms_size + 1;
		int accents_start  = count + 1;
		int accents_size   = int.Parse(lines[count]);
		count += accents_size + 1;
		int sessions_start = count + 1;
		int sessions_size  = int.Parse(lines[count]);
		count += sessions_size + 1;
		int prefix_start   = count + 1;
		int prefix_size    = int.Parse(lines[count]);
		count += prefix_size + 1;
		int lemmas_start   = count + 1;
		int lemmas_size    = int.Parse(lines[count]);
		count += lemmas_size + 1;
		for(int i = lemmas_start; i < count; ++i)
        {
			string r = lines[i];
			string[] refs = r.Split(' ');
			string lemma   = refs[0];
			string par_ref = refs[1];
			string acc_ref = refs[2];
			string ses_ref = refs[3];
			string anc_gen = refs[4];
			Record record = new Record();
			if (par_ref != "-")
				record.paradigm = lines[int.Parse(par_ref)+1];
			if (acc_ref != "-")
				record.accent   = lines[accents_start + int.Parse(acc_ref)+1];
			if (ses_ref != "-")
				record.session  = lines[sessions_start + int.Parse(ses_ref)+1];
			if (anc_gen != "-")
				record.ancode   = anc_gen;
            string[] lems = {lemma};
            var target = Lemma2records;
            if (lemma == "#")
            {
                target = Nolem2records;
                char[] dd = {'%'};
                var ws = new List<string>();
                foreach (string fa in record.paradigm.Split(dd, StringSplitOptions.RemoveEmptyEntries))
                {
                    var w = fa.Split('*')[0];
                    ws.Add(w);
                }
                var hs = new HashSet<string>(ws.ToArray());
                lems = new string[hs.Count];
                hs.CopyTo(lems);
            }
            foreach(string lem in lems)
            {
                if (!target.ContainsKey(lem))
                {
                    target.Add(lem, new List<Record>() { record });
                }
                else
                {
                    var list = target[lem];
                    list.Add(record);
                }
            }
		}
	}

}

}