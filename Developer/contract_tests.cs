using System;
using System.Collections.Generic;
using System.Linq;
public interface IModApi {void InitMod(Mod m);}
public class Mod {}
public class ItemStack {public int count;public ItemValue itemValue=new ItemValue();public bool IsEmpty(){return count==0;}}
public class ItemValue {public ItemClass ItemClass=new ItemClass();}
public class ItemClass {public string Name;public string GetItemName(){return Name;}}
public class XUiController
{
    public V ViewComponent=new V(); public ItemStack ItemStack; public int SlotNumber;
    public event Action<XUiController,int> OnPress;
    public void Click(int button=-1){if(OnPress!=null)OnPress(this,button);}
}
public class V {public string ID,Text,SpriteName,ToolTip;public bool IsVisible=true,Selected;public Vec Size=new Vec(20,722);public Layout Grid;}
public class Vec{public int x,y;public Vec(int a,int b){x=a;y=b;}}
public class Layout
{
    public XUiController[] Controllers;public int Repositions;public bool Broken;
    public void Reposition(){if(Broken){Broken=false;throw new Exception("injected native layout failure");}Repositions++;}
}
public class FakeGrid {public XUiController[] itemControllers;public V ViewComponent;}
public class Drag {public ItemStack CurrentStack=new ItemStack();}
public class FakeUI {public Drag dragAndDrop=new Drag();}
public class Search {public bool showInput;}
public class Scroll
{
    public int page,maxPages=42,inventorySizeX=6,inventorySizeY=8;
    public float scrollBarPixelsPerPage=13;
    public XUiController scrollTab=new XUiController(),scrollBar=new XUiController();
    public FakeGrid Grid;
    public void OnOpen(){maxPages=42;scrollTab.ViewComponent.Size=new Vec(20,176);scrollTab.ViewComponent.SpriteName="ui_scroll_tab_42";}
    public void UpdateScrollBarPosition(){page=Math.Min(page,maxPages);}
    public void UpdateSlots()
    {
        if(!XMWCategories.UpdateSlots(this))return;
        for(int i=0;i<Grid.itemControllers.Length;i++)Grid.itemControllers[i].ViewComponent.IsVisible=i>=page*6 && i<page*6+48;
    }
    public void Wheel(int d){page=Math.Max(0,Math.Min(maxPages,page+d));UpdateSlots();}
}
public class Window
{
    public FakeGrid backpackGrid;public Scroll scrollBar;public Search stackSearch=new Search();public FakeUI xui=new FakeUI();
    public Dictionary<string,XUiController> Controls=new Dictionary<string,XUiController>();
    public Window()
    {
        foreach(string id in XMWCategories.Ids)Controls[id]=new XUiController{ViewComponent=new V{ID=id}};
        foreach(string id in XMWCategories.Ids)Controls[id+"Text"]=new XUiController{ViewComponent=new V{ID=id+"Text"}};
        Controls["xmwCategoryStatus"]=new XUiController{ViewComponent=new V{ID="xmwCategoryStatus"}};
        var slots=new XUiController[300];
        for(int i=0;i<slots.Length;i++)slots[i]=new XUiController{SlotNumber=i,ItemStack=new ItemStack()};
        var layout=new Layout{Controllers=slots};
        backpackGrid=new FakeGrid{itemControllers=slots,ViewComponent=new V{Grid=layout}};
        scrollBar=new Scroll{Grid=backpackGrid};scrollBar.UpdateSlots();
    }
    public XUiController GetChildById(string id){XUiController c;return Controls.TryGetValue(id,out c)?c:null;}
}
namespace HarmonyLib
{
    public class HarmonyMethod{public HarmonyMethod(System.Reflection.MethodInfo x){}}
    public class Harmony{public Harmony(string id){}public void UnpatchSelf(){}public void Patch(System.Reflection.MethodInfo m,HarmonyMethod prefix,HarmonyMethod postfix){}}
    public class AccessTools{public static System.Reflection.MethodInfo Method(Type t,string n,Type[] a){return t.GetMethod(n,a);}}
}
public static class Tests
{
    static int assertions;
    static void Assert(bool x,string m){assertions++;if(!x)throw new Exception(m);}
    static List<int> Visible(Window w){return w.backpackGrid.itemControllers.Where(c=>c.ViewComponent.IsVisible).Select(c=>c.SlotNumber).ToList();}
    static void Select(Window w,int c){w.Controls[XMWCategories.Ids[c]].Click();}
    static void Fill(Window w,int count)
    {
        for(int i=0;i<300;i++){var st=w.backpackGrid.itemControllers[i].ItemStack;st.count=i<count?1:0;st.itemValue.ItemClass.Name=i%2==0?"ammo9mmBulletBall":"foodBaconAndEggs";}
    }
    public static void Main(string[] args)
    {
        XMWCategories.LoadMap();
        if(args.Length==1 && args[0]=="--validate-config")
        {
            Console.WriteLine("PASS config validation; entries="+XMWCategories.MapSize);
            return;
        }
        var w=new Window();Fill(w,120);
        var controllers=(XUiController[])w.backpackGrid.itemControllers.Clone();
        var stacks=controllers.Select(c=>c.ItemStack).ToArray();
        XMWCategories.Bind(w);XMWCategories.Bind(w);XMWCategories.Open(w);
        w.scrollBar.Wheel(4);Assert(Visible(w).First()==24,"native scrolling starts at slot24");
        Select(w,2);
        Assert(w.scrollBar.maxPages==2,"60 matches =>10 rows=>2 scroll steps");
        Assert(Visible(w).SequenceEqual(Enumerable.Range(0,48).Select(i=>i*2)),"first filtered page preserves native indexes");
        w.scrollBar.Wheel(2);
        Assert(Visible(w).SequenceEqual(Enumerable.Range(12,48).Select(i=>i*2)),"last filtered page keeps48 and exact source indexes");
        Select(w,0);Assert(w.scrollBar.page==4 && w.scrollBar.maxPages==42,"All restores original native scroll state");
        Assert(Visible(w).SequenceEqual(Enumerable.Range(24,48)),"All restores native visible slots");
        Select(w,5);Assert(Visible(w).Count==0 && w.scrollBar.maxPages==0,"empty category");
        Assert(!w.scrollBar.scrollTab.ViewComponent.IsVisible,"no divide-by-zero drag target for no pages");
        Select(w,2);w.xui.dragAndDrop.CurrentStack.count=1;
        Select(w,6);Assert(w.Controls[XMWCategories.Ids[2]].ViewComponent.Selected,"category frozen while cursor carries a stack");
        Fill(w,40);XMWCategories.Changed(w.backpackGrid);XMWCategories.Update(w);
        Assert(w.scrollBar.maxPages==2,"no view remapping while dragging");
        w.xui.dragAndDrop.CurrentStack.count=0;XMWCategories.Update(w);
        Assert(Visible(w).Count==20 && w.scrollBar.maxPages==0,"refresh after cursor releases stack");
        w.stackSearch.showInput=true;XMWCategories.Update(w);
        Assert(!w.Controls["xmwCategoryStatus"].ViewComponent.IsVisible,"native search input stays unobstructed");
        XMWCategories.Close(w);Assert(w.scrollBar.maxPages==42,"close restores native layout state");
        XMWCategories.Open(w);Assert(Visible(w).Count==48,"reopen starts All");
        foreach(int n in new[]{0,1,47,48,49,54,55,120,300})
        {
            for(int i=0;i<300;i++){controllers[i].ItemStack.count=i<n?1:0;controllers[i].ItemStack.itemValue.ItemClass.Name="ammo9mmBulletBall";}
            Select(w,2);int max=Math.Max(0,(n+5)/6-8);
            Assert(w.scrollBar.maxPages==max,"boundary maxPages="+n);
            for(int pg=0;pg<=max;pg++)
            {
                w.scrollBar.page=pg;w.scrollBar.UpdateSlots();var visible=Visible(w);
                Assert(visible.SequenceEqual(Enumerable.Range(pg*6,Math.Min(48,n-pg*6))),"boundary visibility="+n+" page="+pg);
            }
            Select(w,0);
        }
        for(int i=0;i<300;i++)
        {
            Assert(object.ReferenceEquals(controllers[i],w.backpackGrid.itemControllers[i]),"controller identity/order unchanged");
            Assert(object.ReferenceEquals(stacks[i],controllers[i].ItemStack) && controllers[i].SlotNumber==i,"stack binding/index unchanged");
        }
        XMWCategories.LoadMap();
        string[] cases={"ulmbookmaintenancecar","ulmbookmaintenancebicycle","ulmbookmaintenancetruck","ulmmeleeaxetitanium","ulmmeleeaxespinebiter","bucketriverwater","drinkjarbeer","ulmdrinkplasticbottlehoney","foodhoney","drugatomjunkies","drugsugarbutts","foodburntsmoothie","foodatomicsmoothie","ulmdrinkcurepoison","gunhandgunt1pistol","ulmresourcebookarmor","ulmresourcebookammo","ulmresourceresearch","ulmresourceresearchguns","ulmresourceskillpointbookfragment","resourcebulletcasing","resourcebullettip","resourcegunpowder","ammo9mmbulletball","ulmammopowercell","ulmammopowercelldepleted","ulmresourcefuelbrick","ulmresourcebrick","ulmresourcecoalpowder","resourcecoal","ulmresourcefirewood","ulmresourceorecopper","ulmresourceingotcopper","ulmresourceelectroniccoppercoil","ulmresourceelectroniccircuitboard","ulmresourcegears","ulmresourcescrapwires","carbattery","ulmresourcelithiumbattery","smallengine","vehiclewheels","ulmresource4x4truckchassis","ulmammofuelcanempty","ulmammofuelgasoline","ulmammogasempty","ulmammogaspropane","resourcecropcoffeebeans","resourcecropcottonplant","resourcecropaloeleaf","ulmresourcequeenbee","ulmresourcescraporganic","modgunmagazineextender","gunhandgunt1pistolparts","resourcerepairkit","ulmresourcebenchvice","questrewardt1skillmagazinebundle","questrewardt1handgunbundle","resourcedoorknob","resourcepaper","resourcesewingkit","meleetoolsalvaget1wrench","ulmresourceingotiron","ulmresourceorezinc","resourcecement","casinocoin"};
        int[] expected={8,8,8,3,3,3,6,6,6,6,6,5,5,5,1,8,8,8,8,8,22,22,24,2,27,27,27,25,24,27,27,20,20,22,22,22,22,27,27,23,23,23,3,27,3,27,6,21,24,26,26,4,22,3,3,8,9,9,21,5,3,20,20,25,9};
        for(int i=0;i<cases.Length;i++)Assert(XMWCategories.ClassifyItem(cases[i])==expected[i],"semantic classification "+cases[i]);
        for(int i=0;i<300;i++){controllers[i].ItemStack.count=i<cases.Length?1:0;controllers[i].ItemStack.itemValue.ItemClass.Name=i<cases.Length?cases[i]:"";}
        Select(w,7);
        Assert(w.Controls["xmwCatAllText"].ViewComponent.Text==XMWCategories.MaterialNames[0],"Material opens submenu using existing labels");
        Assert(Visible(w).SequenceEqual(Enumerable.Range(0,cases.Length).Where(i=>expected[i]>=20)),"All material union excludes books and weapons");
        for(int leaf=20;leaf<=27;leaf++){
            Select(w,leaf-18);
            Assert(Visible(w).SequenceEqual(Enumerable.Range(0,cases.Length).Where(i=>expected[i]==leaf)),"material leaf "+leaf);
        }
        Select(w,0);
        Assert(w.Controls["xmwCatAllText"].ViewComponent.Text==XMWCategories.Names[0],"Back restores main menu labels");
        Select(w,8);Assert(Visible(w).Count==expected.Count(c=>c==8),"books include resource books and research");
        Select(w,7);w.xui.dragAndDrop.CurrentStack.count=1;Select(w,0);
        Assert(w.Controls["xmwCatAllText"].ViewComponent.Text==XMWCategories.MaterialNames[0],"No submenu switch while cursor carries items");
        w.xui.dragAndDrop.CurrentStack.count=0;Select(w,0);
        Assert(XMWCategories.ClassifyItem("bookcaseFromUnknownMod")==9,"unknown bookcase is not guessed as a book");
        Assert(XMWCategories.ClassifyItem("ultraElectronicMetalArmorAddon")==9,"unknown labels are not guessed as raw materials");
        Assert(w.Controls["xmwCatOther"].ViewComponent.ToolTip.Length>0,"main menu explanatory tooltip");
        Select(w,7);
        Assert(w.Controls["xmwCatTool"].ViewComponent.ToolTip.Contains(XMWCategories.Names[21]),"material tooltip switches with submenu");
        Assert(w.Controls["xmwCatAmmoText"].ViewComponent.Text==XMWCategories.MaterialNames[2],"mineral tab uses new naming");
        Select(w,0);
        // Full inventory values and bindings remain byte-for-byte conceptually unchanged.
        for(int i=0;i<300;i++)Assert(controllers[i].SlotNumber==i && object.ReferenceEquals(stacks[i],controllers[i].ItemStack),"submenu slot binding unchanged "+i);
        w.backpackGrid.ViewComponent.Grid.Broken=true;Select(w,2);
        Assert(w.scrollBar.maxPages==42 && Visible(w).Count==48,"native restore after injected layout failure");
        Console.WriteLine("PASS "+assertions+" contract assertions (stub UI, not in-game tests)");
    }
}
