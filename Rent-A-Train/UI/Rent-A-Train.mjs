/* Rent a Train 0.2.5. No bundled React or external dependencies. */
const R = window.React;
const api = window["cs2/api"];
const ui = window["cs2/ui"];
const modding = window["cs2/modding"];
const h = R.createElement;
const icon = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxMjgiIGhlaWdodD0iMTI4IiB2aWV3Qm94PSIwIDAgMTI4IDEyOCI+CiAgPHJlY3Qgd2lkdGg9IjEyOCIgaGVpZ2h0PSIxMjgiIHJ4PSIyMyIgZmlsbD0iIzdiZDNlYyIvPgogIDxwYXRoIGQ9Ik02NCAxM3YxMDIiIHN0cm9rZT0iI2JjZWNmNiIgc3Ryb2tlLXdpZHRoPSIyIi8+CiAgPGNsaXBQYXRoIGlkPSJ0cmFpbi1oYWxmIj48cGF0aCBkPSJNMTAgMTJoNTB2MTA0SDEweiIvPjwvY2xpcFBhdGg+CiAgPGcgY2xpcC1wYXRoPSJ1cmwoI3RyYWluLWhhbGYpIiBmaWxsPSJub25lIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iNyIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIj4KICAgIDxwYXRoIGQ9Ik0zMSA5MVY0N2MwLTE4IDEwLTI3IDI5LTI3czI5IDkgMjkgMjd2NDRjMCA3LTUgMTEtMTIgMTFINDNjLTcgMC0xMi00LTEyLTExeiIvPgogICAgPHBhdGggZD0iTTM0IDQ2aDUydjI0SDM0eiIgZmlsbD0iI2ZmZiIgc3Ryb2tlPSJub25lIi8+CiAgICA8cGF0aCBkPSJNNDYgMzNoMjhNNDQgMTAybC04IDEwbTQwLTEwIDggMTAiLz4KICAgIDxjaXJjbGUgY3g9IjQ0IiBjeT0iODUiIHI9IjQiIGZpbGw9IiNmZmYiIHN0cm9rZT0ibm9uZSIvPgogIDwvZz4KICA8ZyBmaWxsPSJub25lIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iNyIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIj4KICAgIDxwYXRoIGQ9Ik0xMDQgNDBjLTUtNy0yNS03LTI1IDUgMCAxNSAyNSA4IDI1IDI0IDAgMTMtMjAgMTUtMjggNk05MCAyNXY2NSIvPgogIDwvZz4KPC9zdmc+DQo=";
const glyph = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxMjgiIGhlaWdodD0iMTI4IiB2aWV3Qm94PSIwIDAgMTI4IDEyOCI+CiAgCiAgCiAgPGNsaXBQYXRoIGlkPSJ0cmFpbi1oYWxmIj48cGF0aCBkPSJNMTAgMTJoNTB2MTA0SDEweiIvPjwvY2xpcFBhdGg+CiAgPGcgY2xpcC1wYXRoPSJ1cmwoI3RyYWluLWhhbGYpIiBmaWxsPSJub25lIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iNyIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIj4KICAgIDxwYXRoIGQ9Ik0zMSA5MVY0N2MwLTE4IDEwLTI3IDI5LTI3czI5IDkgMjkgMjd2NDRjMCA3LTUgMTEtMTIgMTFINDNjLTcgMC0xMi00LTEyLTExeiIvPgogICAgPHBhdGggZD0iTTM0IDQ2aDUydjI0SDM0eiIgZmlsbD0iI2ZmZiIgc3Ryb2tlPSJub25lIi8+CiAgICA8cGF0aCBkPSJNNDYgMzNoMjhNNDQgMTAybC04IDEwbTQwLTEwIDggMTAiLz4KICAgIDxjaXJjbGUgY3g9IjQ0IiBjeT0iODUiIHI9IjQiIGZpbGw9IiNmZmYiIHN0cm9rZT0ibm9uZSIvPgogIDwvZz4KICA8ZyBmaWxsPSJub25lIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iNyIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIj4KICAgIDxwYXRoIGQ9Ik0xMDQgNDBjLTUtNy0yNS03LTI1IDUgMCAxNSAyNSA4IDI1IDI0IDAgMTMtMjAgMTUtMjggNk05MCAyNXY2NSIvPgogIDwvZz4KPC9zdmc+DQo=";
const initial = {ready:false,busRented:0,trainRented:0,busFleet:0,trainFleet:0,busMaximum:20,trainMaximum:10,busPrice:1000,trainPrice:5000,busCharged:0,trainCharged:0,busNeeded:0,trainNeeded:0,busDepotCapacity:0,trainDepotCapacity:0,status:"",panelVisible:false,useUniversalMenu:false,windowPosition:0};
const state$ = api.bindValue("RentATrain", "state", initial);
const money = value => "$" + Math.round(value).toLocaleString("en-US");
let Slider;

export function RentalApp() {
    const state=api.useValue(state$)||initial;
    const open=!!state.panelVisible;
    const setOpen=value=>api.trigger("RentATrain","setPanelVisible",value);
    const [tab,setTab]=R.useState(0);
    const [drafts,setDrafts]=R.useState({bus:0,train:0}), [confirmed,setConfirmed]=R.useState(false);
    const bus=tab===0, rented=bus?state.busRented:state.trainRented;
    const maximum=bus?state.busMaximum:state.trainMaximum, price=bus?state.busPrice:state.trainPrice;
    const draft=bus?drafts.bus:drafts.train;
    const needed=bus?state.busNeeded:state.trainNeeded;
    const capacity=bus?state.busDepotCapacity:state.trainDepotCapacity;
    const shortfall=Math.max(0,needed-capacity-rented);
    R.useEffect(()=>{
        if(open){setDrafts({bus:state.busRented,train:state.trainRented});setConfirmed(false);}
    },[open]);
    R.useEffect(()=>{
        setDrafts(previous=>({bus:Math.min(previous.bus,state.busMaximum),train:Math.min(previous.train,state.trainMaximum)}));
    },[state.busMaximum,state.trainMaximum]);
    const setQuantity=value=>{
        const quantity=Math.max(0,Math.min(maximum,Math.round(value)));
        if(quantity!==draft){setDrafts(previous=>({...previous,[bus?"bus":"train"]:quantity}));setConfirmed(false);}
    };
    R.useEffect(()=>{api.trigger("RentATrain","setPanelOpen",open);return()=>api.trigger("RentATrain","setPanelOpen",false);},[open]);
    R.useEffect(()=>{
        if(!open)return;
        const close=e=>{if(e.key==="Escape")setOpen(false);};
        window.addEventListener("keydown",close);return()=>window.removeEventListener("keydown",close);
    },[open]);
    const row=(label,value,extra="")=>h("div",{className:"rat-summary-row "+extra},h("span",null,label),h("span",{className:"rat-summary-value"},String(value??0)));
    const error=/^(Cannot|No (compatible|road)|Rental simulation stopped|Duplicate)/.test(state.status||"");
    return h("div",{className:"rat-window-layer"},
        open&&h(PositionedRentalPanel,{mode:state.windowPosition,onClose:()=>setOpen(false),key:"rent-window-"+state.windowPosition},
            h("div",{className:"rat-tabs"},["Buses","Trains"].map((label,index)=>h(ui.Button,{key:label,className:"rat-tab"+(tab===index?" rat-active":""),onSelect:()=>setTab(index)},label))),
            h("div",{className:"rat-body"},
                h("div",{className:"rat-summary"},row("Vehicles needed",needed),row("Depot capacity",capacity),row("Rented",rented),row("Shortfall",shortfall,shortfall?"rat-deficit":"rat-covered")),
                h("div",{className:"rat-quantity"},h("span",null,"Rental quantity"),h("div",{className:"rat-count"},draft+" / "+maximum)),
                h("div",{className:"rat-slider-row"},
                    h(ui.Button,{className:"rat-step",disabled:draft<=0,onSelect:()=>setQuantity(draft-1)},"−"),
                    h(Slider,{className:"rat-slider",start:0,end:maximum,value:draft,valueTransformer:sliderQuantity,onChange:setQuantity}),
                    h(ui.Button,{className:"rat-step",disabled:draft>=maximum,onSelect:()=>setQuantity(draft+1)},"+")),
                h("div",{className:"rat-price-line"},h("span",null,"Per vehicle / month"),h("span",null,money(price))),
                h("div",{className:"rat-total"},h("span",null,"Monthly rental cost"),h("span",{className:"rat-total-value"},money(draft*price))),
                h(ui.Button,{className:"rat-apply",disabled:!state.ready||confirmed||(drafts.bus===state.busRented&&drafts.train===state.trainRented),onSelect:()=>{api.trigger("RentATrain","setRentals",drafts.bus,drafts.train);setConfirmed(true);}},confirmed?"Change confirmed":"Apply"),
                error&&h("div",{className:"rat-error"},state.status)
            )));
}

export function panelInitialPosition(mode,geometry) {
    if(mode!==1)return {x:0.5,y:0.5};
    if(!geometry)return {x:0.005,y:0.08};
    return {x:geometry.left/Math.max(1,geometry.viewportWidth-geometry.width),
        y:geometry.top/Math.max(1,geometry.viewportHeight-geometry.height)};
}
export function PositionedRentalPanel({mode=0,onClose,children}) {
    const container=R.useRef(null),probe=R.useRef(null);
    const [position,setPosition]=R.useState(mode===1?null:{x:0.5,y:0.5});
    R.useEffect(()=>{
        if(mode!==1)return;
        let frame,attempts=0,cancelled=false;
        const measure=()=>{
            if(cancelled)return;
            const panel=container.current?.querySelector('.rat-panel');
            const rect=panel?.getBoundingClientRect(),anchor=probe.current?.getBoundingClientRect();
            if(rect?.width>0&&rect?.height>0&&anchor){
                setPosition(panelInitialPosition(1,{left:anchor.left,top:anchor.top,width:rect.width,height:rect.height,
                    viewportWidth:document.documentElement.clientWidth||window.innerWidth,
                    viewportHeight:document.documentElement.clientHeight||window.innerHeight}));
            }else if(++attempts<10)frame=window.requestAnimationFrame(measure);
            else setPosition(panelInitialPosition(1));
        };
        frame=window.requestAnimationFrame(measure);
        return()=>{cancelled=true;window.cancelAnimationFrame(frame);};
    },[mode]);
    return h('div',{ref:container,className:'rat-panel-anchor',style:{visibility:position?'inherit':'hidden'}},
        h('div',{ref:probe,className:'rat-infoview-position'}),
        h(ui.Panel,{className:'rat-panel',header:'Rent a Train',onClose,draggable:true,
            key:position?'positioned':'measuring',initialPosition:position||panelInitialPosition(mode)},children));
}
export function RentalButton({universal=false}={}) {
    const state=api.useValue(state$)||initial;
    if(!!state.useUniversalMenu!==universal)return null;
    return h(ui.Tooltip,{tooltip:'Rent a Train'},h(ui.Button,{
        src:glyph,tinted:true,variant:universal?'round':'floating',
        selected:!!state.panelVisible,onSelect:()=>api.trigger('RentATrain','setPanelVisible',!state.panelVisible)
    }));
}

// Native Slider calls its transformer as (start, end, normalizedPosition).
export function sliderQuantity(start,end,position) {
    return Math.max(Math.min(start,end),Math.min(Math.max(start,end),Math.round(start+position*(end-start))));
}

// Preserve the exact children shape. TutorialGuideTarget rejects [singleElement].
function mapChildren(children,visit) {
    return Array.isArray(children)?R.Children.map(children,visit):visit(children);
}

// Decorate the existing game's own layout: all native rows and navigation remain.
// Monetary values in Budget follow the game's simulation-day / monthly display convention.
export function decorateBudgetPage(root, busCost, trainCost, classes) {
    let expenseValues, item, selected = false;
    const cost = busCost + trainCost;
    function scan(node) {
        if (!R.isValidElement(node)) return;
        const p = node.props;
        if (Array.isArray(p.items) && Array.isArray(p.values) && p.items.some(i=>i.sources?.some(s=>s.index===14))) {
            expenseValues = [...p.values,-busCost,-trainCost];
            item = {id:"Rented vehicles",active:true,color:{r:0.61,g:0.35,b:0.79,a:1},icon,
                sources:[{id:"Rented buses",index:p.values.length},{id:"Rented trains",index:p.values.length+1}]};
            selected = p.selectedItem === item.id;
        }
        R.Children.forEach(p.children,scan);
    }
    scan(root);
    if (!item) return root;
    function visit(node) {
        if (!R.isValidElement(node)) return node;
        const p = node.props, update = {};
        if (Array.isArray(p.items) && Array.isArray(p.values) && p.items.some(i=>i.sources?.some(s=>s.index===14))) {
            update.items = [...p.items,item]; update.values = expenseValues; update.total = p.total-cost;
        }
        if (typeof p.value === "number" && p.className === classes.totalField) update.value = p.value-cost;
        if (selected && p.className === classes.detailClass && p.item == null) { update.item = item; update.values = expenseValues; }
        if (p.children != null) update.children = mapChildren(p.children,visit);
        return R.cloneElement(node,update);
    }
    return visit(root);
}
export function decorateBudgetChart(root,cost,selectedId) {
    function visit(node) {
        if (!R.isValidElement(node)) return node;
        const p=node.props, update={};
        if (p.data && Array.isArray(p.data.datasets)) update.data={...p.data,datasets:[...p.data.datasets,
            {label:"Rented vehicles",data:[cost],backgroundColor:"rgba(156,89,201,1)",stack:"expenses",barPercentage:selectedId==="Rented vehicles"?0.9:0.5,categoryPercentage:0.7}]};
        if (p.children != null) update.children=mapChildren(p.children,visit);
        return R.cloneElement(node,update);
    }
    return visit(root);
}
// A failure in this optional extension must never take down the game's UI root.
class BudgetBoundary extends R.Component {
    constructor(props){super(props);this.state={failed:false};}
    static getDerivedStateFromError(){return {failed:true};}
    componentDidCatch(error){console.error("[Rent a Train] Budget extension failed; showing native budget.",error);}
    render(){return this.state.failed?this.props.fallback:this.props.children;}
}
export default function register(registry) {
    Slider=modding.getModule("game-ui/common/input/slider/slider.tsx","Slider");
    registry.append("GameTopLeft",()=>h(RentalButton,{universal:false}));
    registry.append("UniversalModMenu",()=>h(RentalButton,{universal:true}));
    registry.append("Game",RentalApp);
    const classes=modding.getModule("game-ui/game/components/economy-panel/budget-page/budget-page.module.scss","classes");
    registry.extend("game-ui/game/components/economy-panel/budget-page/budget-page.tsx","BudgetPage",Original=>{
        function Decorated(props){
            const state=api.useValue(state$)||initial;
            return decorateBudgetPage(Original(props),state.busRented*state.busPrice,state.trainRented*state.trainPrice,{totalField:classes.totalField,detailClass:classes.infoColumn});
        }
        return props=>h(BudgetBoundary,{fallback:h(Original,props)},h(Decorated,props));
    });
    registry.extend("game-ui/game/components/economy-panel/budget-page/budget-chart/budget-chart.tsx","BudgetChart",Original=>{
        function Decorated(props){
            const state=api.useValue(state$)||initial;
            return decorateBudgetChart(Original(props),state.busRented*state.busPrice+state.trainRented*state.trainPrice,props.selectedId);
        }
        return props=>h(BudgetBoundary,{fallback:h(Original,props)},h(Decorated,props));
    });
}
export const hasCSS=true;
