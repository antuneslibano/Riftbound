using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Riftbound.Config;
static class Dump {
  static object Val(object v){
    if (v==null) return null;
    var t=v.GetType();
    if (t.IsEnum) return Convert.ToInt32(v);
    if (v is bool b) return b?1:0;
    if (v is float f) return f;
    if (v is int i) return i;
    if (v is string s) return s;
    if (v is CardDefinition cd) return new Dictionary<string,object>{{"$ref",cd.cardId}};
    var d=new List<object[]>();
    foreach(var fi in t.GetFields(BindingFlags.Public|BindingFlags.Instance)) d.Add(new object[]{fi.Name, Val(fi.GetValue(v))});
    return d;
  }
  static object Top(CardDefinition c){
    var d=new List<object[]>();
    d.Add(new object[]{"m_Name", c.name});
    foreach(var fi in typeof(CardDefinition).GetFields(BindingFlags.Public|BindingFlags.Instance)) d.Add(new object[]{fi.Name, Val(fi.GetValue(c))});
    return d;
  }
  public static void Run(){
    var cards=DefaultContent.CreateCards(out var sp);
    var all=new List<object>();
    all.Add(Top(sp)); foreach(var c in cards) all.Add(Top(c));
    Console.WriteLine(JsonSerializer.Serialize(all));
  }
}
