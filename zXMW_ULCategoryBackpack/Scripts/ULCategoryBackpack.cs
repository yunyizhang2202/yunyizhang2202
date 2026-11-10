// v0.3.4-rc1. UL 2.7.40 / 7DTD V2.6 b14. In-game acceptance pending.
// Inventory controllers, ItemStack, SlotNumber, Bag and controller order are never changed.
// Only native IsVisible and ULM scrollbar presentation state change. Native UIGrid lays out views.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;

public sealed class XMWULCategoryBackpack : IModApi
{
    public void InitMod(Mod instance)
    {
        Harmony h = new Harmony("xmw.ul.backpack.categories.v034");
        try
        {
            XMWCategories.LoadMap();
            Type w = XMWUtil.RequireType("XUiC_ULM_BackpackWindow");
            Type g = XMWUtil.RequireType("XUiC_ULM_BackpackGrid");
            Type s = XMWUtil.RequireType("ULM_StackScrollBar");
            Add(h,w,"Init",Type.EmptyTypes,"Init",false);
            Add(h,w,"OnOpen",Type.EmptyTypes,"Open",false);
            Add(h,w,"OnClose",Type.EmptyTypes,"Close",true);
            Add(h,w,"Update",new Type[]{typeof(float)},"Update",false);
            Add(h,g,"SetStacks",new Type[]{typeof(ItemStack[])},"Changed",false);
            Add(h,s,"UpdateSlots",Type.EmptyTypes,"Slots",true);
            XMWUtil.Log("v0.3.4-rc1 patches ready; no global Pressed patch; map="+XMWCategories.MapSize);
        }
        catch(Exception ex)
        {
            h.UnpatchSelf();
            XMWUtil.Log("INIT FAILED; patches removed: "+ex);
        }
    }
    private static void Add(Harmony h,Type t,string name,Type[] args,string callback,bool prefix)
    {
        MethodInfo m=AccessTools.Method(t,name,args);
        if(m==null)throw new MissingMethodException(t.FullName,name);
        HarmonyMethod cb=new HarmonyMethod(typeof(XMWULCategoryBackpack).GetMethod(callback));
        h.Patch(m,prefix?cb:null,prefix?null:cb);
    }
    public static void Init(object __instance){XMWCategories.Bind(__instance);}
    public static void Open(object __instance){XMWCategories.Open(__instance);}
    public static void Close(object __instance){XMWCategories.Close(__instance);}
    public static void Update(object __instance){XMWCategories.Update(__instance);}
    public static void Changed(object __instance){XMWCategories.Changed(__instance);}
    public static bool Slots(object __instance){return XMWCategories.UpdateSlots(__instance);}
}

internal static class XMWCategories
{
    internal sealed class State
    {
        public object Window,Grid,Scroll,Layout;
        public XUiController[] Slots;
        public int Category,NativePage;
        public bool Open,Dirty,Failed,Refreshing,MaterialMenu;
        public readonly List<int> Results=new List<int>();
        public readonly HashSet<XUiController> Visible=new HashSet<XUiController>();
        public readonly List<XUiController> Buttons=new List<XUiController>();
        public readonly List<object> Labels=new List<object>();
        public int Columns,Rows;
    }
    private static readonly Dictionary<object,State> Windows=new Dictionary<object,State>();
    private static readonly Dictionary<XUiController,State> Owners=new Dictionary<XUiController,State>();
    private static readonly Dictionary<string,int> Map=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
    internal static readonly string[] Ids={"xmwCatAll","xmwCatWeapon","xmwCatAmmo","xmwCatTool","xmwCatGear","xmwCatMedical","xmwCatFood","xmwCatMaterial","xmwCatBooks","xmwCatOther"};
    internal static readonly string[] Names={"\u5168\u90e8","\u6b66\u5668","\u5f39\u836f","\u5de5\u5177","\u88c5\u5907","\u533b\u7597","\u996e\u98df","\u6750\u6599","\u77e5\u8bc6","\u5176\u4ed6","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","\u77ff\u77f3\u952d\u6599","\u57fa\u7840\u8f85\u6599","\u5de5\u4e1a\u96f6\u4ef6","\u8f7d\u5177\u96f6\u4ef6","\u5316\u5de5\u7528\u54c1","\u5efa\u6750\u8bbe\u65bd","\u519c\u7267\u79cd\u690d","\u71c3\u6599\u80fd\u6e90"};
    internal static readonly string[] MaterialNames={"\u8fd4\u56de","\u5168\u6750","\u77ff\u952d","\u57fa\u6750","\u96f6\u4ef6","\u8f66\u4ef6","\u5316\u5de5","\u5efa\u9020","\u519c\u7267","\u80fd\u6e90"};
    private static readonly int[] MaterialIds={0,7,20,21,22,23,24,25,26,27};
    private static readonly string[] Help={"\u663e\u793a\u6240\u6709\u7269\u54c1\uff0c\u4e0d\u7b5b\u9009\uff0c\u6062\u590d\u539f\u751f\u6eda\u52a8\u3002","\u6210\u54c1\u6b66\u5668\uff1a\u67aa\u68b0\u3001\u5f13\u5f29\u548c\u8fd1\u6218\u6b66\u5668\u3002\u67aa\u68b0\u96f6\u4ef6\u5728\u6750\u6599\uff0c\u56fe\u7eb8\u5728\u77e5\u8bc6\u3002","\u53ef\u76f4\u63a5\u4f7f\u7528\u7684\u5b50\u5f39\u3001\u7bad\u77e2\u548c\u7206\u70b8\u7269\u3002\u5f39\u58f3\u3001\u5f39\u5934\u5728\u96f6\u4ef6\uff0c\u706b\u836f\u5728\u5316\u5de5\u3002","\u91c7\u96c6\u3001\u62c6\u89e3\u3001\u7ef4\u4fee\u5de5\u5177\uff1b\u4e5f\u5305\u62ec\u7a7a\u6cb9\u7f50\u3001\u7a7a\u74f6\u3001\u6c34\u6876\u3001\u7ef4\u4fee\u5305\u548c\u5f00\u9501\u7528\u54c1\u3002","\u7a7f\u6234\u7684\u62a4\u7532\u3001\u8863\u7269\uff0c\u4ee5\u53ca\u53ef\u76f4\u63a5\u5b89\u88c5\u7684\u6539\u88c5\u914d\u4ef6\u3002\u5236\u9020\u914d\u4ef6\u7684\u96f6\u4ef6\u5728\u6750\u6599\u3002","\u7ef7\u5e26\u3001\u5939\u677f\u3001\u836f\u7269\u3001\u6ce8\u5c04\u5242\u548c\u6297\u5371\u5bb3\u8840\u6e05\u3002\u8702\u871c\u4e0e\u589e\u76ca\u7cd6\u679c\u653e\u5728\u996e\u98df\u3002","\u98df\u7269\u3001\u996e\u6599\u3001\u53a8\u623f\u539f\u6599\u548c\u589e\u76ca\u7cd6\u679c\u3002\u5496\u5561\u8c46\u662f\u98df\u6750\uff0c\u79cd\u5b50\u5728\u519c\u7267\u3002","\u6253\u5f00\u6750\u6599\u5b50\u5206\u7c7b\uff1b\u7535\u5b50\u548c\u673a\u68b0\u90e8\u4ef6\u5408\u5e76\u5728\u5de5\u4e1a\u96f6\u4ef6\uff0c\u4e0d\u6309\u6750\u8d28\u91cd\u590d\u5206\u7c7b\u3002","\u4e66\u7c4d\u3001\u6280\u80fd\u4e66\u3001\u7ef4\u4fee\u624b\u518c\u3001\u56fe\u7eb8\u3001\u7814\u7a76\u6570\u636e\u548c\u4efb\u52a1\u6587\u4ef6\u3002\u4e0d\u5305\u542b\u7a7a\u767d\u7eb8\u5f20\u3002","\u6574\u8f86\u8f7d\u5177\u3001\u8d27\u5e01\u8d35\u91cd\u54c1\u3001\u6df7\u5408\u793c\u5305\u548c\u5f85\u56de\u6536\u7269\uff1b\u672a\u8bc6\u522b\u7269\u54c1\u4e5f\u5728\u8fd9\u91cc\u3002","","","","","","","","","","","\u77ff\u77f3\u952d\u6599\uff1a\u77ff\u77f3\u3001\u91d1\u5c5e\u952d\u3001\u677f\u6750\u548c\u7194\u70bc\u5e9f\u6599\u3002\u9f7f\u8f6e\u548c\u7535\u8def\u677f\u4e0d\u5728\u8fd9\u91cc\u3002","\u57fa\u7840\u8f85\u6599\uff1a\u6728\u77f3\u3001\u7802\u571f\u3001\u5e03\u6599\u3001\u76ae\u9769\u3001\u7eb8\u5f20\u3001\u73bb\u7483\u3001\u5851\u6599\u3001\u7ef3\u7d22\u7b49\u901a\u7528\u6750\u6599\u3002","\u5de5\u4e1a\u96f6\u4ef6\uff1a\u9f7f\u8f6e\u3001\u5f39\u7c27\u3001\u7535\u8def\u677f\u3001\u94dc\u7ebf\u5708\u7b49\u673a\u7535\u90e8\u4ef6\uff0c\u4ee5\u53ca\u5236\u67aa\u3001\u5236\u5f39\u96f6\u4ef6\u3002","\u8f7d\u5177\u96f6\u4ef6\uff1a\u8f6e\u80ce\u3001\u5e95\u76d8\u3001\u5ea7\u6905\u3001\u8f66\u628a\u3001\u5f15\u64ce\u548c\u8f66\u706f\u3002\u7535\u6c60\u5728\u80fd\u6e90\uff0c\u6574\u8f86\u8f66\u5728\u5176\u4ed6\u3002","\u5316\u5de5\u7528\u54c1\uff1a\u9178\u3001\u80f6\u6c34\u3001\u706b\u836f\u3001\u7164\u7c89\u3001\u6cb9\u6599\u3001\u6d82\u6599\u7b49\u914d\u65b9\u539f\u6599\u3002\u6c7d\u6cb9\u7b49\u71c3\u6599\u5728\u80fd\u6e90\u3002","\u5efa\u6750\u8bbe\u65bd\uff1a\u6c34\u6ce5\u3001\u6df7\u51dd\u571f\u3001\u7816\u3001\u52a0\u56fa\u4ef6\uff0c\u4ee5\u53ca\u53ef\u653e\u7f6e\u7684\u5bb6\u5177\u548c\u5de5\u4f5c\u53f0\u3002","\u519c\u7267\u79cd\u690d\uff1a\u79cd\u5b50\u3001\u8702\u540e\u3001\u6d3b\u9e21\u548c\u80a5\u6599\u3002\u6536\u83b7\u7684\u98df\u6750\u5728\u996e\u98df\uff0c\u68c9\u82b1\u5728\u57fa\u6750\u3002","\u71c3\u6599\u80fd\u6e90\uff1a\u6c7d\u6cb9\u3001\u4e19\u70f7\u3001\u7164\u3001\u6728\u70ad\u3001\u71c3\u6599\u7816\u3001\u7535\u6c60\u548c\u592a\u9633\u80fd\u90e8\u4ef6\u3002\u7a7a\u6cb9\u7f50\u5728\u5de5\u5177\u3002"};
    private static readonly HashSet<string> Unknown=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public static int MapSize {get{return Map.Count;}}
    public static void LoadMap()
    {
        Map.Clear(); Unknown.Clear();
        ReadMap("CategoryMap.tsv",false,false);
        ReadMap("MaterialMap.tsv",true,true);
        ReadMap("CategoryOverrides.tsv",true,false);
        foreach(string key in new List<string>(Map.Keys))if(Map[key]==7)Map[key]=21;
        XMWUtil.Log("Classification loaded: "+Map.Count+" entries; material submenu enabled");
    }
    private static void ReadMap(string file,bool optional,bool leafOnly)
    {
        string path=Path.Combine(XMWUtil.Folder,"Config/"+file);
        if(optional && !File.Exists(path))return;
        int lineNo=0;
        foreach(string raw in File.ReadAllLines(path))
        {
            lineNo++; string line=raw.Trim();
            if(line.Length==0 || line.StartsWith("#"))continue;
            string[] a=line.Split('\t');int c;
            if(a.Length!=2 || a[0].Trim().Length==0 || !int.TryParse(a[1],out c) || c<1 || (c>9 && (c<20 || c>27)))
                throw new InvalidDataException(file+":"+lineNo+" invalid category row");
            string key=a[0].Trim();
            if(leafOnly)
            {
                int main;
                if(c<20 || c>27 || !Map.TryGetValue(key,out main) || main!=7)
                    throw new InvalidDataException(file+":"+lineNo+" leaf must refer to a material item");
            }
            Map[key]=c;
        }
    }
    internal static int ClassifyItem(string name)
    {
        int result;
        if(Map.TryGetValue(name??"",out result))return result;
        result=Classify(name);
        if(Unknown.Count<200 && Unknown.Add(name??""))
            XMWUtil.Log("UNMAPPED item="+name+" fallback="+result);
        return result;
    }
    private static void Menu(State s)
    {
        for(int i=0;i<s.Labels.Count;i++)
        {
            int c=s.MaterialMenu?MaterialIds[i]:i;
            XMWUtil.Set(s.Labels[i],"Text",s.MaterialMenu?MaterialNames[i]:Names[i]);
            // Tooltip is presentation only: unsupported view types must never disable filtering.
            TryTooltip(s.Labels[i],Help[c]);
            TryTooltip(XMWUtil.Get(s.Buttons[i],"ViewComponent"),Help[c]);
        }
    }
    private static void TryTooltip(object view,string text)
    {
        try {XMWUtil.Set(view,"ToolTip",text);} catch {}
    }
    public static void Bind(object window)
    {
        if(Windows.ContainsKey(window))return;
        State s=new State(); s.Window=window;
        Windows.Add(window,s);
        try
        {
            foreach(string id in Ids)
            {
                XUiController b=(XUiController)XMWUtil.Call(window,"GetChildById",new object[]{id});
                if(b==null)throw new InvalidOperationException("Missing category button: "+id);
                object text=XMWUtil.Call(window,"GetChildById",new object[]{id+"Text"});
                if(text==null)throw new InvalidOperationException("Missing category label: "+id+"Text");
                s.Labels.Add(XMWUtil.Get(text,"ViewComponent"));
                b.OnPress+=Pressed; s.Buttons.Add(b); Owners.Add(b,s);
            }
            XMWUtil.Log("Bound 10 local category OnPress handlers");
        }
        catch(Exception e){s.Failed=true;XMWUtil.Log("BIND FAILED: "+e);}
    }
    public static void Open(object window)
    {
        if(!Windows.ContainsKey(window))Bind(window);
        State s=Windows[window]; s.Open=true; s.Category=0; s.Dirty=false; s.MaterialMenu=false;
        if(s.Failed)return;
        try
        {
            Menu(s);
            s.Grid=XMWUtil.Get(window,"backpackGrid"); s.Scroll=XMWUtil.Get(window,"scrollBar");
            if(s.Grid==null || s.Scroll==null)throw new InvalidOperationException("Missing native grid/scrollbar");
            IEnumerable raw=(IEnumerable)XMWUtil.Get(s.Grid,"itemControllers");
            List<XUiController> slots=new List<XUiController>();
            foreach(object c in raw)slots.Add((XUiController)c);
            s.Slots=slots.ToArray();
            object view=XMWUtil.Get(s.Grid,"ViewComponent");
            s.Layout=XMWUtil.Get(view,"Grid");
            s.Columns=Convert.ToInt32(XMWUtil.Get(s.Scroll,"inventorySizeX"));
            s.Rows=Convert.ToInt32(XMWUtil.Get(s.Scroll,"inventorySizeY"));
            if(s.Layout==null || s.Columns<=0 || s.Rows<=0 || s.Slots.Length<s.Columns*s.Rows)
                throw new InvalidOperationException("Native layout not ready");
            // Confirm compatibility before any visibility changes.
            XMWUtil.RequireMember(s.Scroll,"page"); XMWUtil.RequireMember(s.Scroll,"maxPages");
            XMWUtil.RequireMember(s.Scroll,"scrollBarPixelsPerPage");
            XMWUtil.Log("OPEN slots="+s.Slots.Length+" columns="+s.Columns+" visibleRows="+s.Rows);
            Label(s,"全部");
        }
        catch(Exception e){Fail(s,e);}
    }
    public static void Close(object window)
    {
        State s; if(!Windows.TryGetValue(window,out s))return;
        try {if(s.Category!=0)Restore(s);}catch(Exception e){XMWUtil.Log("RESTORE FAILED: "+e);}
        s.Open=false; s.Category=0; s.MaterialMenu=false; s.Dirty=false; s.Results.Clear();s.Visible.Clear();
    }
    private static void Pressed(XUiController button,int mouseButton)
    {
        State s; if(mouseButton!=-1 || !Owners.TryGetValue(button,out s) || !s.Open || s.Failed)return;
        try
        {
            if(Holding(s)){XMWUtil.Log("Category change deferred: cursor holds a stack");return;}
            int index=Array.IndexOf(Ids,Convert.ToString(XMWUtil.Get(XMWUtil.Get(button,"ViewComponent"),"ID")));
            if(index<0)throw new InvalidOperationException("Unknown bound button ID");
            int c=s.MaterialMenu?MaterialIds[index]:index;
            if(s.MaterialMenu && index==0)s.MaterialMenu=false;
            else if(!s.MaterialMenu && c==7)s.MaterialMenu=true;
            Menu(s);
            XMWUtil.Log("PRESS category="+c+" materialMenu="+s.MaterialMenu);
            if(s.Category==0 && c!=0)s.NativePage=Convert.ToInt32(XMWUtil.Get(s.Scroll,"page"));
            if(c==0){if(s.Category!=0)Restore(s);Label(s,"全部");return;}
            s.Category=c; s.Dirty=true; XMWUtil.Set(s.Scroll,"page",0); Refresh(s);
        }
        catch(Exception e){Fail(s,e);}
    }
    public static void Changed(object grid)
    {
        foreach(State s in Windows.Values)if(s.Open && object.ReferenceEquals(s.Grid,grid))s.Dirty=true;
    }
    public static void Update(object window)
    {
        State s;if(!Windows.TryGetValue(window,out s) || !s.Open || s.Failed)return;
        try
        {
            object status=XMWUtil.Call(s.Window,"GetChildById",new object[]{"xmwCategoryStatus"});
            if(status!=null)XMWUtil.Set(XMWUtil.Get(status,"ViewComponent"),"IsVisible",!Convert.ToBoolean(XMWUtil.Get(XMWUtil.Get(window,"stackSearch"),"showInput")));
            if(s.Category!=0 && s.Dirty && !Holding(s))Refresh(s);
        }
        catch(Exception e){Fail(s,e);}
    }
    private static bool Holding(State s)
    {
        object ui=XMWUtil.Get(s.Window,"xui");
        object drag=XMWUtil.Get(ui,"dragAndDrop");
        object stack=XMWUtil.Get(drag,"CurrentStack");
        if(stack==null)throw new InvalidOperationException("Cannot inspect drag cursor; filtering disabled");
        return !Convert.ToBoolean(XMWUtil.Call(stack,"IsEmpty",new object[0]));
    }
    private static void Refresh(State s)
    {
        if(s.Refreshing)return;
        s.Refreshing=true;
        try
        {
            s.Results.Clear();
            for(int i=0;i<s.Slots.Length;i++)
            {
                object stack=XMWUtil.Get(s.Slots[i],"ItemStack");
                if(stack==null || Convert.ToInt32(XMWUtil.Get(stack,"count"))<=0)continue;
                object value=XMWUtil.Get(stack,"itemValue");
                object cls=XMWUtil.Get(value,"ItemClass");
                string name=Convert.ToString(XMWUtil.Call(cls,"GetItemName",new object[0]));
                int c=ClassifyItem(name);
                if(c==s.Category || (s.Category==7 && c>=20 && c<=27))s.Results.Add(i);
            }
            int max=Math.Max(0,(s.Results.Count+s.Columns-1)/s.Columns-s.Rows);
            XMWUtil.Set(s.Scroll,"maxPages",max);
            int page=Math.Max(0,Math.Min(max,Convert.ToInt32(XMWUtil.Get(s.Scroll,"page"))));
            XMWUtil.Set(s.Scroll,"page",page);
            object tab=XMWUtil.Get(XMWUtil.Get(s.Scroll,"scrollTab"),"ViewComponent");
            object bar=XMWUtil.Get(XMWUtil.Get(s.Scroll,"scrollBar"),"ViewComponent");
            // Same thumb geometry as UL's own OnOpen, without changing grid dimensions/capacity.
            object size=XMWUtil.Get(bar,"Size");
            int height=Convert.ToInt32(XMWUtil.Get(size,"y"));
            float pixels=Convert.ToSingle(XMWUtil.Get(s.Scroll,"scrollBarPixelsPerPage"));
            if(max>0 && (pixels<=0 || height<=max*pixels))throw new InvalidOperationException("Invalid scrollbar geometry");
            if(max>0)
            {
                XMWUtil.Set(tab,"Size",Activator.CreateInstance(size.GetType(),new object[]{20,height-(int)(max*pixels)}));
                XMWUtil.Set(tab,"SpriteName","ui_scroll_tab_"+max);
            }
            XMWUtil.Set(tab,"IsVisible",max>0);
            XMWUtil.Call(s.Scroll,"UpdateScrollBarPosition",new object[0]);
            ApplyVisibility(s); s.Dirty=false;
            Label(s,Names[s.Category]+" · "+s.Results.Count);
            XMWUtil.Log("FILTER category="+s.Category+" matches="+s.Results.Count+" row="+page+" max="+max);
        }
        finally{s.Refreshing=false;}
    }
    public static bool UpdateSlots(object scrollbar)
    {
        foreach(State s in Windows.Values)
        {
            if(!s.Open || s.Failed || s.Category==0 || !object.ReferenceEquals(s.Scroll,scrollbar))continue;
            try{ApplyVisibility(s);return false;}
            catch(Exception e){Fail(s,e);return true;}
        }
        return true;
    }
    private static void ApplyVisibility(State s)
    {
        int page=Convert.ToInt32(XMWUtil.Get(s.Scroll,"page"));
        int start=page*s.Columns, end=Math.Min(s.Results.Count,start+s.Columns*s.Rows);
        s.Visible.Clear();
        for(int i=start;i<end;i++)s.Visible.Add(s.Slots[s.Results[i]]);
        foreach(XUiController c in s.Slots)XMWUtil.Set(c.ViewComponent,"IsVisible",s.Visible.Contains(c));
        // Native UIGrid hides inactive children, compacts and updates collider geometry itself.
        // No per-slot Transform writes and no reparenting/reordering.
        XMWUtil.Call(s.Layout,"Reposition",new object[0]);
    }
    private static void Restore(State s)
    {
        s.Category=0;s.Dirty=false;
        if(s.Scroll==null)return;
        object tab=XMWUtil.Get(XMWUtil.Get(s.Scroll,"scrollTab"),"ViewComponent");
        XMWUtil.Set(tab,"IsVisible",true);
        XMWUtil.Call(s.Scroll,"OnOpen",new object[0]);
        XMWUtil.Set(s.Scroll,"page",Math.Max(0,Math.Min(s.NativePage,Convert.ToInt32(XMWUtil.Get(s.Scroll,"maxPages")))));
        XMWUtil.Call(s.Scroll,"UpdateScrollBarPosition",new object[0]);
        XMWUtil.Call(s.Scroll,"UpdateSlots",new object[0]);
        if(s.Layout!=null)XMWUtil.Call(s.Layout,"Reposition",new object[0]);
        s.Results.Clear();s.Visible.Clear();
    }
    private static void Label(State s,string text)
    {
        object c=XMWUtil.Call(s.Window,"GetChildById",new object[]{"xmwCategoryStatus"});
        if(c!=null)XMWUtil.Set(XMWUtil.Get(c,"ViewComponent"),"Text",text);
        for(int i=0;i<s.Buttons.Count;i++)XMWUtil.Set(XMWUtil.Get(s.Buttons[i],"ViewComponent"),"Selected",(s.MaterialMenu?MaterialIds[i]:i)==s.Category);
    }
    private static void Fail(State s,Exception e)
    {
        XMWUtil.Log("FILTER FAILED; restoring native backpack: "+e);
        try{Restore(s);}catch(Exception restore){XMWUtil.Log("RESTORE FAILED; restart without this mod: "+restore);}
        s.Failed=true;s.Category=0;s.MaterialMenu=false;
        try{Menu(s);Label(s,"ALL (filter disabled)");}catch{}
    }
    private static int Classify(string name)
    {
        // Full known registry is mapped. Unknown third-party items stay visible in Other
        // and are logged, rather than guessed from words such as book, metal or ammo.
        return 9;
    }

}

internal static class XMWUtil
{
    public static string Folder{get{return Path.GetDirectoryName(typeof(XMWULCategoryBackpack).Assembly.Location);}}
    public static void Log(string message)
    {try{File.AppendAllText(Path.Combine(Folder,"XMW_Diagnostics.log"),DateTime.Now.ToString("s")+" "+message+Environment.NewLine);}catch{}}
    public static Type RequireType(string name)
    {
        foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){Type t=a.GetType(name,false);if(t!=null)return t;}
        throw new TypeLoadException(name);
    }
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
    public static MemberInfo RequireMember(object obj,string name)
    {
        if(obj==null)throw new NullReferenceException("Reading "+name);
        for(Type t=obj.GetType();t!=null;t=t.BaseType)
        {
            PropertyInfo p=t.GetProperty(name,Flags);if(p!=null)return p;
            FieldInfo f=t.GetField(name,Flags);if(f!=null)return f;
        }
        throw new MissingMemberException(obj.GetType().FullName,name);
    }
    public static object Get(object obj,string name)
    {MemberInfo m=RequireMember(obj,name);PropertyInfo p=m as PropertyInfo;return p!=null?p.GetValue(obj,null):((FieldInfo)m).GetValue(obj);}
    public static void Set(object obj,string name,object value)
    {MemberInfo m=RequireMember(obj,name);PropertyInfo p=m as PropertyInfo;if(p!=null)p.SetValue(obj,value,null);else((FieldInfo)m).SetValue(obj,value);}
    public static object Call(object obj,string name,object[] args)
    {
        if(obj==null)throw new NullReferenceException("Calling "+name);
        for(Type t=obj.GetType();t!=null;t=t.BaseType)
            foreach(MethodInfo m in t.GetMethods(Flags))
                if(m.Name==name && m.GetParameters().Length==args.Length)return m.Invoke(obj,args);
        throw new MissingMethodException(obj.GetType().FullName,name);
    }
}
