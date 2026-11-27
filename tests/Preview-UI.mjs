import fs from 'node:fs';
const state={ready:true,panelVisible:true,windowPosition:0,trainRented:8,busRented:0,trainMaximum:10,busMaximum:20,trainPrice:5000,busPrice:1000,trainNeeded:14,busNeeded:0,trainDepotCapacity:4,busDepotCapacity:0,status:''};
let hook=0,app;
const h=(type,props,...children)=>({type,props:{...props,children:children.length===1?children[0]:children}});
const React={createElement:h,Component:class{},useState:()=>[[1,{bus:0,train:8},false][hook++],()=>{}],useEffect:()=>{},useRef:()=>({current:null})};
const Panel=props=>h('section',{className:props.className},h('header',{},props.header),props.children);
const Slider=props=>h('input',{type:'range',min:props.start,max:props.end,value:props.value,className:props.className});
globalThis.window={React,'cs2/api':{bindValue:()=>({}),useValue:()=>state},'cs2/ui':{Button:'button',Panel},'cs2/modding':{getModule:(_p,key)=>key==='Slider'?Slider:{}}};
const {RentalApp:appComponent}=await import('../Rent-A-Train/UI/Rent-A-Train.mjs');
app=appComponent;
const escape=x=>String(x).replaceAll('&','&amp;').replaceAll('"','&quot;').replaceAll('<','&lt;');
function render(node){
    if(node==null||typeof node==='boolean')return '';
    if(Array.isArray(node))return node.map(render).join('');
    if(typeof node!=='object')return escape(node);
    if(typeof node.type==='function')return render(node.type(node.props));
    const attrs=Object.entries(node.props).filter(([k,v])=>!['children','key'].includes(k)&&!k.startsWith('on')&&typeof v!=='object').map(([k,v])=>' '+(k==='className'?'class':k)+'="'+escape(v)+'"').join('');
    return '<'+node.type+attrs+'>'+render(node.props.children)+'</'+node.type+'>';
}
const css=fs.readFileSync(new URL('../Rent-A-Train/UI/Rent-A-Train.css',import.meta.url),'utf8');
const html='<!doctype html><meta charset="utf-8"><style>html{font-size:1.33px}body{margin:20px;background:#435748;font-family:Arial;color:white}button{border:1px solid #b9d4df;cursor:pointer}section{position:absolute;top:25px;left:75px;border-radius:10px;overflow:hidden}header{background:#101b23;text-align:center;padding:14px;color:#56ccef;font-size:20px}input{padding:0;width:100px}'+css+'</style>'+render(app());
fs.writeFileSync(process.argv[2],html);

