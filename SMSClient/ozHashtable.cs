using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace SMSClient
{
  
    public class ozHashtable : Hashtable 
    {
        public void fromUnparsedParams(String paramlist)
        {
            fromUnparsedParams(paramlist, false, false); 
        }

        public void fromUnparsedParams(String paramlist, bool multiselect, bool multiarray) 
        {
            Encoding enc = null;
            String[] keyvals = paramlist.Split('&');
            for (int x = 0; x < keyvals.Length; x++)
            {
                if (keyvals[x].StartsWith("_charset_"))
                {
                    string[] parts = keyvals[x].Split(new char[] { '=' }, 2);
                    if ((parts[0] == "_charset_") && (parts.Length == 2))
                    {
                        if ((parts[1] != "") && (parts[1].ToUpper(System.Globalization.CultureInfo.InvariantCulture) != "UNKNOWN"))
                        {
                            try
                            {
                                enc = System.Text.Encoding.GetEncoding(parts[1]);
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
            if (enc == null) enc = Encoding.UTF8;
            for (int x = 0; x < keyvals.Length; x++)
            {
                if (((string)keyvals[x]).Contains("="))
                {
                    String[] parts = keyvals[x].Split('=');
                    //String name = ozCommonStrings.urlDecode(parts[0], enc);
                    String name = parts[0];
                    String val = ozHTTPUtility.UrlDecode(parts[1], enc);
                    if (this.ContainsKey(name))
                    {
                        if ( ((this[name] is string) &&  (((string)this[name]).Length == 0)) || (!multiselect))
                        {
                            this[name] = val;
                        }
                        else
                        {
                            if (multiarray)
                            {
                                if (this[name] is string)
                                {
                                    string oldvalue = (string)this[name];
                                    string[] nArr = new string[1];
                                    nArr[0] = oldvalue;
                                    this[name] = nArr;
                                }

                                if (this[name] is string[])
                                {
                                    string[] oldArray = (string[])this[name];
                                    string[] newArray = new string[oldArray.Length + 1];
                                    oldArray.CopyTo(newArray, 0);
                                    newArray[newArray.Length - 1] = val;
                                    this[name] = newArray;
                                }
                            }
                            else
                            {
                                val = val.Replace("\\", "\\\\").Replace(";", "\\;");
                                this[name] += ";" + val;
                                //A checkboxoknál a default value-t mindig hamarabb küldjük be, mert
                                //ha nincs bepipálva nem jön semmi
                                //nem szép, de nincs más megoldás
                                if ((string)this[name] == "on;off") { this[name] = "off"; }
                                if ((string)this[name] == "on;on") { this[name] = "on"; }
                                if ((string)this[name] == "off;off") { this[name] = "off"; }
                                if ((string)this[name] == "off;on") { this[name] = "on"; }
                                
                            }
                        }
                    }
                    else
                    {
                        this.Add(name, val);
                    }
                }
            }
        }

        public void fromUnparsedParamsUnicode(string paramlist)
        {
            string[] keyvals = paramlist.Split('&');
            for (int x = 0; x < keyvals.Length; x++)
            {
                if (keyvals[x].Contains("="))
                {
                    string[] parts = keyvals[x].Split('=');
                    if (this.ContainsKey(parts[0]))
                    {
                        this[parts[0]] = parts[1];
                    }
                    else
                    {
                        this.Add(parts[0], parts[1]);
                    }
                }
            }
        }

   

        public string getStringVal(string keyName)
        {
            if (this.ContainsKey(keyName))
            {
                return (string)this[keyName];
            }
            else
            {
                return "";
            }
        }

        public bool containsKeyStartsWith(string pattern)
        {
            foreach (object key in this.Keys)
            {
                if (key.ToString().IndexOf(pattern) > -1) return true;
            }
            return false;
        }


        public ozHashtable viewMatching(string pattern)
        {
            ozHashtable retTable = new ozHashtable();
            Regex re = new Regex(pattern);
            foreach (string key in this.Keys)
            {
                if (re.IsMatch(key))
                {
                    retTable.Add(key, this[key]);
                }
            }
            return retTable;
        }

        public ozHashtable replaceKeys(string pattern,string replaceto)
        {
            ozHashtable retTable = new ozHashtable();
            Regex re = new Regex(pattern);
            foreach (string key in this.Keys)
            {
                retTable.Add(key.Replace(pattern,replaceto), this[key]);
            }
            return retTable;
        }

        public void setVal(object key, object value)
        {
            if (this.ContainsKey(key))
            {
                this[key] = value;
            }
            else
            {
                Add(key, value);
            }
        }

        public ozHashtable convertKeysToLowercase()
        {
            ozHashtable retTable = new ozHashtable();
            foreach (string key in this.Keys)
            {
                retTable.Add(key.ToLower(), this[key]);
            }
            return retTable;
        }

        public bool save(string fileName, out string errorMessage)
        {
            try
            {
                TextWriter configFile = new StreamWriter(fileName);
                foreach (string key in this.Keys)
                {
                    string line = key + "=" + (string)this[key];
                    configFile.WriteLine(line);
                }
                configFile.Close();
                errorMessage = "";
                return true;
            } catch (Exception exp) {
                errorMessage = "Cannot save file '" + fileName + "'." + exp.Message;
                return false;
            }
        }

        public bool load(string fileName, out string errorMessage)
        {
            try
            {
                TextReader configFile = new StreamReader(fileName);
                string line = "";
                while (line != null) {
                    line = configFile.ReadLine();
                    if (line != null)
                    {
                        if (line.Contains("="))
                        {
                            string[] name_val = line.Split(new char[] { '=' }, 2);
                            string key = name_val[0].Trim();
                            string val = name_val[1].Trim();
                            Add(key, val);
                        }
                    }
                }
                configFile.Close();
                errorMessage = "";
                return true;
            }
            catch (Exception exp)
            {
                errorMessage = "Cannot load file '" + fileName + "'." + exp.Message;
                return false;
            }
        }

    }
}

