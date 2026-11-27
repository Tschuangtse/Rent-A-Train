import fs from "node:fs";
import assert from 'node:assert/strict';
const React={
    Component:class {constructor(props){this.props=props;}},
    createElement(type,props,...children){return {type,props:{...props,children:children.length===1?children[0]:children}};},
    isValidElement(node){return !!node && typeof node==='object' && 'type' in node && 'props' in node;},
    cloneElement(node,update){return {...node,props:{...node.props,...update}};},
    Children:{forEach(value,fn){(Array.isArray(value)?value:[value]).flat(Infinity).forEach(fn);},map(value,fn){return (Array.isArray(value)?value:[value]).flat(Infinity).map(fn);}}
};
globalThis.window={React,'cs2/api':{bindValue(){return {}; }},'cs2/ui':{},'cs2/modding':{getModule(path,key){return key==='classes'?{totalField:'balance',infoColumn:'detail'}:()=>null;}}};
const {decorateBudgetPage,decorateBudgetChart,default:register,RentalApp,RentalButton,PositionedRentalPanel,panelInitialPosition,sliderQuantity}=await import('../Rent-A-Train/UI/Rent-A-Train.mjs');
const h=React.createElement;
const nativeValues=Array(15).fill(0); nativeValues[5]=-1000; nativeValues[14]=-200;
const items=[{id:'Service Upkeep',sources:[{index:5}]},{id:'Tile Upkeep',sources:[{index:14}]}];
const root=h('page',{},h('section',{items,values:nativeValues,total:-1200,selectedItem:'Rented vehicles'}),h('money',{className:'balance',value:800}),h('detail',{className:'detail',item:null,values:nativeValues}));
const result=decorateBudgetPage(root,2000,15000,{totalField:'balance',detailClass:'detail'});
const [section,balance,detail]=result.props.children;
assert.equal(section.props.items.at(-1).id,'Rented vehicles');
assert.equal(section.props.total,-18200);
assert.deepEqual(section.props.values.slice(-2),[-2000,-15000]);
assert.deepEqual(section.props.items.at(-1).sources.map(s=>s.index),[15,16]);
assert.equal(balance.props.value,-16200);
assert.equal(detail.props.item.id,'Rented vehicles');
assert.equal(detail.props.values[16],-15000);
assert.equal(nativeValues.length,15); assert.equal(items.length,2);
assert.equal(root.props.children[1].props.value,800);
console.log('PASS: Budget row, expense total, balance, details and separate vehicle costs');
console.log('PASS: Native budget data stays unchanged (no duplicate treasury charge)');
const empty=decorateBudgetPage(root,0,0,{totalField:'balance',detailClass:'detail'});
assert.equal(empty.props.children[0].props.total,-1200);
assert.equal(empty.props.children[1].props.value,800);
console.log('PASS: Zero rental leaves budget totals unchanged');
const chart=h('frame',{},h('chart',{data:{labels:['Budget'],datasets:[{label:'Taxes',data:[2000]}]}}));
const adjusted=decorateBudgetChart(chart,17000,'Rented vehicles');
const dataset=adjusted.props.children.props.data.datasets.at(-1);
assert.equal(dataset.data[0],17000); assert.equal(dataset.stack,'expenses'); assert.equal(dataset.barPercentage,0.9);
assert.equal(chart.props.children.props.data.datasets.length,1);
console.log('PASS: Expense chart includes rent and preserves native series');
const calls=[];
register({append:(...args)=>calls.push(['append',...args]),extend:(...args)=>calls.push(['extend',...args])});
assert.equal(calls[0][1],'GameTopLeft');
assert.equal(calls[1][1],'UniversalModMenu');
assert.equal(calls[2][1],'Game');
assert.equal(calls[2][2],RentalApp);
assert.equal(calls.filter(c=>c[0]==='extend').length,2);
console.log('PASS: Toolbar, BudgetPage and BudgetChart registrations');
// Regression: React.Children.map(singleChild) returns an array. The game's
// TutorialGuideTarget explicitly rejects that shape, bringing down the UI root.
function TutorialGuideTarget(props) {
    assert.equal(React.isValidElement(props.children),true,'TutorialGuideTarget needs ONE element, not an array');
    return props.children;
}
const guide=h(TutorialGuideTarget,{uiTag:'budget'},h('label',{},'Budget'));
const regressionRoot=h('page',{},root,guide);
const safe=decorateBudgetPage(regressionRoot,2000,15000,{totalField:'balance',detailClass:'detail'});
const preserved=safe.props.children[1];
TutorialGuideTarget(preserved.props);
assert.equal(preserved.props.children.props.children,'Budget');
const chartGuide=h(TutorialGuideTarget,{uiTag:'chart'},chart);
const safeChart=decorateBudgetChart(chartGuide,17000,null);
TutorialGuideTarget(safeChart.props);
console.log('PASS: Budget and chart preserve single-element and text children (reported UI crash regression)');
const extend=calls.find(c=>c[2]==='BudgetPage');
const wrapper=extend[3](()=>h('native-budget',{}));
const protectedTree=wrapper({});
const Boundary=protectedTree.type;
assert.deepEqual(Boundary.getDerivedStateFromError(new Error('render failure')),{failed:true});
const boundary=new Boundary(protectedTree.props);
boundary.state={failed:true};
assert.equal(boundary.render(),protectedTree.props.fallback);
console.log('PASS: A budget render failure falls back to native budget inside a local error boundary');
const source=fs.readFileSync(new URL('../Rent-A-Train/UI/Rent-A-Train.mjs',import.meta.url),'utf8');
assert.match(source,/data:image\/svg\+xml;base64,/);
assert.doesNotMatch(source,/coui:\/\/ui-mods\/Rent-A-Train\/rent-a-train.svg/);
assert.doesNotMatch(source,/RENTAL FLEET|Resume the simulation|Rental limits and prices/);
console.log('PASS: Self-contained icon and compact menu without removed helper text');
console.log('All UI integration checks passed. Actual Cohtml rendering remains an in-game test.');

assert.equal(sliderQuantity(0,10,0.5),5);
assert.equal(sliderQuantity(0,30,0.75),23);
assert.equal(sliderQuantity(0,10,1),10);
assert.equal(sliderQuantity(0,10,-0.2),0);
console.log('PASS: Slider track clicks use start/end/position and cannot collapse to start');

// Stateful UI harness: exercise real handlers and effects across tab switches.
let cells=[],effectDeps=[],pendingEffects=[],stateIndex=0,effectIndex=0;
let live={ready:true,panelVisible:false,useUniversalMenu:false,windowPosition:0,busRented:0,trainRented:0,busMaximum:20,trainMaximum:10,busPrice:1000,trainPrice:5000,status:''};
const messages=[];
React.useState=initial=>{
    const index=stateIndex++;
    if(!(index in cells))cells[index]=typeof initial==='function'?initial():initial;
    return [cells[index],value=>{cells[index]=typeof value==='function'?value(cells[index]):value;}];
};
React.useEffect=(fn,deps)=>{
    const index=effectIndex++,old=effectDeps[index];
    if(!old||deps.some((value,i)=>!Object.is(value,old[i])))pendingEffects.push(fn);
    effectDeps[index]=deps;
};
window.addEventListener=()=>{};window.removeEventListener=()=>{};
window['cs2/api'].useValue=()=>live;
window['cs2/api'].trigger=(...args)=>{messages.push(args);if(args[1]==='setPanelVisible')live={...live,panelVisible:args[2]};};
function render(){
    let tree;
    for(let pass=0;pass<2;pass++){
        stateIndex=effectIndex=0;pendingEffects=[];tree=RentalApp();
        for(const effect of pendingEffects)effect();
    }
    return tree;
}
function find(tree,cls){
    if(Array.isArray(tree)){for(const child of tree){const found=find(child,cls);if(found)return found;}return null;}
    if(!tree||typeof tree!=='object')return null;
    if((tree.props?.className||'').split(' ').includes(cls))return tree;
    return find(tree.props?.children,cls);
}
function tab(tree,index){return find(tree,'rat-tabs').props.children[index];}
let tree=render();RentalButton().props.children.props.onSelect();tree=render();
find(tree,'rat-slider').props.onChange(5);tree=render();
assert.equal(find(tree,'rat-count').props.children,'5 / 20');
tab(tree,1).props.onSelect();tree=render();
find(tree,'rat-slider').props.onChange(8);tree=render();
tab(tree,0).props.onSelect();tree=render();
assert.equal(find(tree,'rat-count').props.children,'5 / 20');
find(tree,'rat-apply').props.onSelect();tree=render();
assert.deepEqual(messages.filter(args=>args[1]==='setRentals').at(-1),['RentATrain','setRentals',5,8]);
assert.equal(find(tree,'rat-apply').props.children,'Change confirmed');
tab(tree,1).props.onSelect();tree=render();
assert.equal(find(tree,'rat-apply').props.children,'Change confirmed');
assert.equal(find(tree,'rat-count').props.children,'8 / 10');
find(tree,'rat-slider').props.onChange(9);tree=render();
assert.equal(find(tree,'rat-apply').props.children,'Apply');
// Server has accepted the last confirmed pair; close discards only the draft 9.
live={...live,busRented:5,trainRented:8};
RentalButton().props.children.props.onSelect();tree=render();
RentalButton().props.children.props.onSelect();tree=render();
assert.equal(find(tree,'rat-count').props.children,'8 / 10');
console.log('PASS: Tab drafts persist, Apply submits both, confirmation persists until editing, and closing discards unconfirmed edits');
assert.equal(RentalButton().props.children.props.variant,'floating');
assert.equal(RentalButton({universal:true}),null);
live={...live,useUniversalMenu:true};
assert.equal(RentalButton(),null);
assert.equal(RentalButton({universal:true}).props.children.props.variant,'round');
assert.deepEqual(panelInitialPosition(0),{x:0.5,y:0.5});
assert.ok(panelInitialPosition(1).x<0.02 && panelInitialPosition(1).y<0.1);
React.useRef=()=>({current:null});
stateIndex=effectIndex=0;cells=[];
const positioned=PositionedRentalPanel({mode:0,onClose:()=>{},children:null});
const panel=positioned.props.children[1];
assert.equal(panel.props.draggable,true);
assert.deepEqual(panel.props.initialPosition,{x:0.5,y:0.5});
assert.equal(find(tree,'rat-window-layer')!=null,true);
assert.ok(!calls.some(c=>c[1].includes('toolbar-button-strip')));
assert.match(source,/Monthly rental cost/);
assert.doesNotMatch(source,/Daily rental cost|Per vehicle \/ day/);
console.log('PASS: Mutually exclusive infoview/Universal menu entries and an independent centered draggable window');
console.log('PASS: Optional upper-left position and monthly pricing labels');

const geometry={left:13.333,top:74.667,width:426,height:580,viewportWidth:1920,viewportHeight:1080};
const pos=panelInitialPosition(1,geometry);
assert.ok(Math.abs(pos.x*(1920-426)-geometry.left)<0.001);
assert.ok(Math.abs(pos.y*(1080-580)-geometry.top)<0.001);
const larger={...geometry,viewportWidth:2560,viewportHeight:1440};
const largerPos=panelInitialPosition(1,larger);
assert.ok(Math.abs(largerPos.y*(1440-580)-geometry.top)<0.001);
const glyphMatch=source.match(/const glyph = "data:image\/svg\+xml;base64,([^"]+)"/);
assert.ok(glyphMatch);
assert.doesNotMatch(Buffer.from(glyphMatch[1],'base64').toString(),/<rect/);
const css=fs.readFileSync(new URL('../Rent-A-Train/UI/Rent-A-Train.css',import.meta.url),'utf8');
assert.doesNotMatch(css,/rat-toolbar-button|rat-toolbar-icon/);
assert.match(css,/top: calc\(10rem \+ var\(--floatingToggleSize\) \+ 6rem\)/);
console.log('PASS: Upper-left gap uses measured native coordinates at multiple resolutions, with native button dimensions and a transparent menu glyph');
