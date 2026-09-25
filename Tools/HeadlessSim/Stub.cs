using System;
namespace UnityEngine {
  public class Object { public string name { get; set; } public static bool operator ==(Object a, Object b) => ReferenceEquals(a,b); public static bool operator !=(Object a, Object b) => !ReferenceEquals(a,b); public override bool Equals(object o)=>ReferenceEquals(this,o); public override int GetHashCode()=>base.GetHashCode(); }
  public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject, new() => new T(); }
  public static class Resources { public static T Load<T>(string p) where T : Object => null; }
  public static class Debug { public static void LogWarning(object o) => Console.WriteLine("WARN " + o); public static void Log(object o) => Console.WriteLine(o); }
  public class HeaderAttribute : Attribute { public HeaderAttribute(string s){} }
  public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
  public class RangeAttribute : Attribute { public RangeAttribute(float a,float b){} }
  public class MinAttribute : Attribute { public MinAttribute(float a){} }
  public class TextAreaAttribute : Attribute { }
  public class SerializeField : Attribute { }
  public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; public int order; }
  public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>new Color(1,1,1); public static Color black=>new Color(0,0,0); public static Color Lerp(Color x, Color y, float t)=>new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t); }
  public struct Vector2 {
    public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
    public static Vector2 zero=>new Vector2(0,0); public static Vector2 one=>new Vector2(1,1);
    public float sqrMagnitude=>x*x+y*y; public float magnitude=>(float)Math.Sqrt(x*x+y*y);
    public Vector2 normalized { get { float m=magnitude; return m>1e-5f? new Vector2(x/m,y/m): zero; } }
    public static Vector2 operator +(Vector2 a, Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
    public static Vector2 operator -(Vector2 a, Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
    public static Vector2 operator -(Vector2 a)=>new Vector2(-a.x,-a.y);
    public static Vector2 operator *(Vector2 a, float d)=>new Vector2(a.x*d,a.y*d);
    public static Vector2 operator *(float d, Vector2 a)=>new Vector2(a.x*d,a.y*d);
    public static Vector2 operator /(Vector2 a, float d)=>new Vector2(a.x/d,a.y/d);
    public static bool operator ==(Vector2 a, Vector2 b)=>(a-b).sqrMagnitude<1e-10f; public static bool operator !=(Vector2 a, Vector2 b)=>!(a==b);
    public override bool Equals(object o)=>o is Vector2 v && v==this; public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode();
    public static float Distance(Vector2 a, Vector2 b)=>(a-b).magnitude;
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t){t=Mathf.Clamp01(t);return a+(b-a)*t;}
    public static Vector2 MoveTowards(Vector2 c, Vector2 t, float maxD){ Vector2 d=t-c; float m=d.magnitude; if(m<=maxD||m==0) return t; return c+d/m*maxD; }
    public override string ToString()=>$"({x:0.00},{y:0.00})";
  }
  public static class Mathf {
    public const float PI=(float)Math.PI; public const float Rad2Deg=57.29578f;
    public static float Clamp(float v,float a,float b)=>v<a?a:v>b?b:v; public static int Clamp(int v,int a,int b)=>v<a?a:v>b?b:v;
    public static float Clamp01(float v)=>Clamp(v,0,1); public static float Min(float a,float b)=>a<b?a:b; public static float Max(float a,float b)=>a>b?a:b;
    public static int Min(int a,int b)=>a<b?a:b; public static int Max(int a,int b)=>a>b?a:b;
    public static float Abs(float a)=>Math.Abs(a); public static int Abs(int a)=>Math.Abs(a); public static float Sqrt(float a)=>(float)Math.Sqrt(a);
    public static float Sign(float a)=>a>=0?1:-1; public static float Cos(float a)=>(float)Math.Cos(a); public static float Sin(float a)=>(float)Math.Sin(a);
    public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);
    public static float MoveTowards(float c,float t,float m)=>Math.Abs(t-c)<=m?t:c+Sign(t-c)*m;
    public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
    public static int FloorToInt(float f)=>(int)Math.Floor(f); public static int CeilToInt(float f)=>(int)Math.Ceiling(f); public static int RoundToInt(float f)=>(int)Math.Round(f);
    public static float PingPong(float t,float l){ t%= (2*l); return l-Math.Abs(t-l);} 
  }
}
