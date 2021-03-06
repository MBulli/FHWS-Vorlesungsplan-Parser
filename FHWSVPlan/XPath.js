﻿// array.js
// (C) 2009 henrik.lindqvist@llamalab.com

(function(ap){if(!ap.every){ap.every=function(fn,thisp){for(var l=this.length,i=0;--l>=0;i++)
if(i in this&&!fn.call(thisp,this[i],i,this))return false;return true;};}
if(!ap.filter){ap.filter=function(fn,thisp){var r=[];for(var l=this.length,i=0,v;--l>=0;i++)
if(i in this&&fn.call(thisp,v=this[i],i,this))r.push(v);return r;};}
if(!ap.forEach){ap.forEach=function(fn,thisp){for(var l=this.length,i=0;--l>=0;i++)
if(i in this)fn.call(thisp,this[i],i,this);};}
if(!ap.indexOf){ap.indexOf=function(e,i){var l=this.length;i=(i<0)?Math.ceil(i):(i>0)?Math.floor(i):0;if(i<0)i+=l;for(;i<l;i++)
if(i in this&&this[i]===e)return i;return-1;};}
if(!ap.lastIndexOf){ap.lastIndexOf=ap.lastIndexOf=function(e,i){var l=this.length;if(isNaN(i))i=l-1;else{i=(i<0)?Math.ceil(i):Math.floor(i);if(i<0)i+=l;else if(i>=l)i=l-1;}
for(;i>=0;i--)
if(i in this&&this[i]===e)return i;return-1;};}
if(!ap.map){ap.map=function(fn,thisp){var l=this.length,r=new Array(l);for(var i=0;--l>=0;i++)
if(i in this)r[i]=fn.call(thisp,this[i],i,this);return r;};}
if(!ap.some){ap.some=function(fn,thisp){for(var l=this.length,i=0;--l>=0;i++)
if(i in this&&fn.call(thisp,this[i],i,this))
return true;return false;};}
['join','concat','pop','push','reverse','shift','slice','sort','splice','unshift','indexOf','lastIndexOf','filter','forEach','every','map','some'].forEach(function(n){if(!Array[n])
Array[n]=new Function('return Function.prototype.call.apply(Array.prototype.'+n+', arguments)');});})(Array.prototype);


// (C) 2009 henrik.lindqvist@llamalab.com
// xpath.js

(function(w,f){function XPath(e){this.e=e;this.i=0;this.js=['with(XPath){return ','}'];this.expression(1,1)||this.error();return new Function('n','nsr',this.js.join(''));}
XPath.ie=/MSIE/.test(navigator.userAgent);XPath.prototype={match:function(rx,x){var m,r;if(!(m=rx.exec(this.e.substr(this.i)))||(typeof x=='number'&&!(r=m[x]))||(typeof x=='object'&&!(r=x[m[1]])))return false;this.m=m;this.i+=m[0].length;return r||m;},error:function(m){m=(m||'Syntax error')+' at index '+this.i+': '+this.e.substr(this.i);var e;try{e=new XPathException(51,m)}
catch(x){e=new Error(m)}
throw e;},step:function(l,r,s,n){var i=3;if(this.match(/^(\/\/?|\.\.?|@)\s*/,1)){switch(this.m[1]){case'/':if(s)this.error();if(!n)return this.step(l,r,1);this.js.splice(l,0,' axis(axes["','document-root','"],');i+=this.nodeTypes.node.call(this,l+i);s=1;break;case'//':if(s)this.error();this.js.splice(l,0,' axis(axes["','descendant-or-self','"],');i+=this.nodeTypes.node.call(this,l+i);s=1;break;case'.':if(!s&&!n)this.error();this.js.splice(l,0,' axis(axes["','self','"],');i+=this.nodeTypes.node.call(this,l+i);s=0;break;case'..':if(!s&&!n)this.error();this.js.splice(l,0,' axis(axes["','parent','"],');i+=this.nodeTypes.node.call(this,l+i);s=0;break;case'@':if(!s&&!n)this.error();this.js.splice(l,0,' axis(axes["','attribute','"],');i+=this.nodeTest(l+i,'node')||this.error('Missing nodeTest after @');s=0;}}
else if(!s&&!n)return s?this.error():0;else if(this.match(/^([a-z]+(?:-[a-z]+)*)\s*::\s*/,XPath.axes)){this.js.splice(l,0,' axis(axes["',this.m[1],'"],');i+=this.nodeTest(l+i,(this.m[1]=='attribute')?'node':'element')||this.error('Missing nodeTest after ::');s=0;}
else if(i=this.nodeTest(l,'element')){this.js.splice(l,0,' axis(axes["','child','"],');i+=3;s=0;}
else return 0;for(var j;j=this.predicate(l+i);i+=j);if(n)this.js.splice(r+i++,0,n);i+=this.step(l,r+i,s);this.js.splice(r+i++,0,')');return i;},expression:function(l,r,p){var o,i=this.operand(l);while(o=this.match(/^(or|and|!?=|[<>]=?|[|*+-]|div|mod)\s*/,this.operators)){if(p&&p[0]>=o[0]){this.i-=this.m[0].length;break;}
this.js.splice(l,0,o[1]);i++;this.js.splice(l+i++,0,o[2]);i+=this.expression(l+i,r,o)||this.error('Missing operand');this.js.splice(l+i++,0,o[3]);}
return i;},operand:function(l){if(this.match(/^(-?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)|"[^"]*"|'[^']*')\s*/,1)){this.js.splice(l,0,this.m[1]);return 1;}
var fn;if(fn=this.match(/^([a-z]+(?:-[a-z]+)*)\s*\(\s*/,this.functions)){var i=1,j;this.js.splice(l,0,fn[1]);do{if(j)this.js.splice(l+i++,0,',');i+=(j=this.expression(l+i,l+i));}while(j&&this.match(/^,\s*/));this.match(/^\)\s*/)||this.error('Missing (');if(fn[0]){if(j)this.js.splice(l+i++,0,',');this.js.splice(l+i++,0,fn[0]);}
if(fn[2])this.js.splice(l+i++,0,fn[2]);else if(j>1)this.error('Function has arguments');i+=this.step(l,l+i);return i;}
if(this.match(/^\(\s*/)){var i=1;this.js.splice(l,0,'(');i+=this.expression(l+i,l+i);this.match(/^\)\s*/)||this.error('Missing )');this.js.splice(l+i++,')');return i;}
return this.step(l,l,0,'[n]');},operators:{'|':[1,'union(',',',')'],'or':[1,'bool(',')||bool(',')'],'and':[2,'bool(',')&&bool(',')'],'=':[3,'compare(eq,',',',')'],'!=':[3,'compare(ne,',',',')'],'<':[4,'compare(lt,',',',')'],'>':[4,'compare(gt,',',',')'],'<=':[4,'compare(le,',',',')'],'>=':[4,'compare(ge,',',',')'],'+':[5,'number(',')+number(',')'],'-':[5,'number(',')-number(',')'],'*':[6,'number(',')*number(',')'],'div':[6,'number(',')/number(',')'],'mod':[6,'number(',')%number(',')']},functions:{'last':[0,'nl.length'],'position':[0,'(i+1)'],'count':['nl','(','.length||0)'],'id':['n','id(',')'],'local-name':['nl','localName(',')'],'namespace-uri':['nl','namespaceURI(',')'],'name':['nl','qName(',')'],'string':['n','string(',')'],'concat':[0,'concat(',')'],'starts-with':[0,'startsWith(',')'],'contains':[0,'contains(',')'],'substring-before':[0,'substringBefore(',')'],'substring-after':[0,'substringAfter(',')'],'substring':[0,'substring(',')'],'string-length':['n','string(',').length'],'normalize-space':['n','normalizeSpace(',')'],'translate':[0,'translate(',')'],'boolean':[0,'bool(',')'],'not':[0,'!bool(',')'],'true':[0,'true '],'false':[0,'false '],'number':['n','number(',')'],'floor':[0,'Math.floor(number(','))'],'ceiling':[0,'Math.ceil(number(','))'],'round':[0,'Math.round(number(','))'],'sum':[0,'sum(',')']},predicate:function(l){var i=0;if(this.match(/^\[\s*/)){if(i=this.expression(l,l)){this.js.splice(l,0,'function(n,i,nl){with(XPath){var r=');i++;this.js.splice(l+i++,0,';return typeof r=="number"?Math.round(r)==i+1:bool(r)}},');}
this.match(/^\]\s*/)||this.error('Missing ]');}
return i;},nodeTest:function(l,t){var fn;if(fn=this.match(/^([a-z]+(?:-[a-z]+)*)\(([^)]*)\)\s*/,this.nodeTypes))
return fn.call(this,l,this.m[2]);if(this.match(/^\*\s*/))
return this.nodeTypes[t].call(this,l);return this.nodeName(l)},nodeType:function(l,t){this.js.splice(l,0,'function(n){return n.nodeType==',t,'},');return 3;},nodeTypes:{'node':function(l){this.js.splice(l,0,'null,');return 1;},'element':function(l){return this.nodeType(l,1);},'attribute':function(l){return this.nodeType(l,2);},'text':function(l){return this.nodeType(l,3);},'processing-instruction':function(l,t){if(!t)return this.nodeType(l,7);this.js.splice(l,0,'function(n){return n.nodeType==7&&n.target==',t,'},');return 3;},'comment':function(l){return this.nodeType(l,8);}},nodeName:function(l){if(!this.match(/^([a-zA-Z_]+(?:-?[a-zA-Z0-9]+)*)(?::([a-zA-Z_]+(?:-?[a-zA-Z0-9]+)*))?\s*/,1))
return 0;if(this.m[2]){this.js.splice(l,0,'function(n){if(!nsr)throw new XPathException(14);return "',this.m[2],'"==',XPath.ie?'n.baseName':'n.localName','&&nsr.lookupNamespaceURI("',this.m[1],'")==n.namespaceURI},');return 7;}
else{this.js.splice(l,0,'function(n){return/^',this.m[1],'$/i.test(n.nodeName)},');return 3;}}};XPath.order=function(l,r){var x=l.compareDocumentPosition?l.compareDocumentPosition(r):XPath.compareDocumentPosition.call(l,r);if(x&32){l=Array.prototype.indexOf.call(l.attributes,l);r=Array.prototype.indexOf.call(r.attributes,r);return(l<r)?-1:(l>r)?1:0;}
if(!x){if(l==r)
return 0;if((l=l.ownerElement)&&(r=r.ownerElement))
return XPath.order(l,r);return XPath.ie?1:0;}
return 3-((x&6)||3);};XPath.compare=function(fn,l,r){if(l instanceof Array&&r instanceof Array){var ls=l.map(this.string),rs=r.map(this.string);for(l=ls.length;--l>=0;)
for(r=rs.length;--r>=0;)
if(!fn(ls[l],rs[r]))return false;return true;}
if(l instanceof Array){for(var i=l.length;--i>=0;)
if(!fn(this[typeof r](l[i]),r))return false;return l.length>0;}
if(r instanceof Array){for(var i=r.length;--i>=0;)
if(!fn(l,this[typeof l](r[i])))return false;return r.length>0;}
if(typeof l=='boolean'||typeof r=='boolean')
return fn(this.bool(l),this.bool(r));if(typeof l=='number'||typeof r=='number')
return fn(this.number(l),this.number(r));return fn(this.string(l),this.string(r));};XPath.eq=function(l,r){return l==r};XPath.ne=function(l,r){return l!=r};XPath.lt=function(l,r){return l<r};XPath.gt=function(l,r){return l>r};XPath.le=function(l,r){return l<=r};XPath.ge=function(l,r){return l>=r};XPath.id=function(s,n){if(arguments.length==1)n=s;var nl=[];for(var id=this.string(s).split(/\s+/),i=id.length;--i>=0;)
if(s=(n.ownerDocument||n).getElementById(id[i]))
nl.push(s);return nl.sort(this.order);};XPath.localName=new Function('nl','return (nl.length&&nl[0].'+(XPath.ie?'baseName':'localName')+')||""');XPath.namespaceURI=function(nl){return(nl.length&&nl[0].namespaceURI)||'';};XPath.qName=function(nl){return(nl.length&&nl[0].nodeName)||'';};XPath.union=function(a,b){if(!a.length)return b;if(!b.length)return a;var nl=[],i=a.length-1,j=b.length-1;for(;;){switch(this.order(a[i],b[j])){case-1:nl.unshift(b[j--]);break;case 0:j--;case 1:nl.unshift(a[i--]);break;default:throw new Error('Invalid order');}
if(i<0){if(++j>0)nl.unshift.apply(nl,nl.slice.call(b,0,j));break;}
if(j<0){if(++i>0)nl.unshift.apply(nl,nl.slice.call(a,0,i));break;}}
return nl;};XPath.string=XPath.object=function(v){if(v instanceof Array&&typeof(v=v[0])=='undefined')return'';if(typeof v=='string')return v;switch(v.nodeType){case 1:case 9:case 11:return Array.prototype.map.call(v.childNodes,this.string,this).join('');default:return v.nodeValue||'';}
return String(v);};XPath.concat=function(){return Array.prototype.map.call(arguments,this.string,this).join('');};XPath.startsWith=function(a,b){return this.string(a).substr(0,(b=this.string(b)).length)==b;};XPath.contains=function(a,b){return this.string(a).indexOf(this.string(b))!=-1;};XPath.substringBefore=function(a,b){a=this.string(a);b=a.indexOf(this.string(b));return b!=-1?a.substr(0,b):'';};XPath.substringAfter=function(a,b){a=this.string(a);b=this.string(b);var i=a.indexOf(b);return i!=-1?a.substr(i+b.length):'';};XPath.substring=function(s,i,l){s=this.string(s);i=Math.round(this.number(i))-1;return(arguments.length==2)?s.substr(i<0?0:i):s.substr(i<0?0:i,Math.round(this.number(l))-Math.max(0,-i));};XPath.normalizeSpace=function(s){return this.string(s).replace(/^\s+/,'').replace(/\s+$/,'').replace(/\s+/g,' ');};XPath.translate=function(a,b,c){a=this.string(a);b=this.string(b);c=this.string(c);var o=[],l=a.length,i=0,j,x;while(--l>=0)
if((j=b.indexOf(x=a.charAt(i++)))==-1||(x=c.charAt(j)))o.push(x);return o.join('');};XPath.bool=XPath['boolean']=function(v){if(typeof v=='boolean')return v;if(v instanceof Array||typeof v=='string')return v.length>0;return Boolean(v);};XPath.number=function(v){if(v instanceof Array&&typeof(v=v[0])=='undefined')return 0;if(typeof v=='number')return v;if(typeof v=='boolean')return v?1:0;return Number(this.string(v));};XPath.sum=function(nl){var r=0,i=nl.length;while(--i>=0)r+=this.number(nl[i]);return r;};XPath.walk=function(n,nl){var x,c=n.firstChild;while(c){nl.push(c);if(x=c.firstChild)c=x;else for(x=c;!(c=x.nextSibling)&&(x=x.parentNode)&&(x!=n););}
return nl;};XPath.axes={'ancestor':function(n){var nl=[];while(n=n.parentNode)nl.unshift(n);return nl;},'ancestor-or-self':function(n){var nl=[];do{nl.unshift(n)}while(n=n.parentNode);return nl;},'attribute':new Function('n','var nl = [], a = n.attributes;if(a){attr:for(var x,i=a.length;--i>=0;){if(!(x=a[i]).specified){'+
(XPath.ie?'switch(x.nodeName){case"selected":case"value":if(x.nodeValue)break;default:continue attr;}':'continue;')+'}nl.unshift(x);}}return nl;'),'child':function(n){return n.childNodes||[];},'descendant':function(n){return this.walk(n,[]);},'descendant-or-self':function(n){return this.walk(n,[n]);},'following':function(n){var nl=[],x;while(n){if(x=n.nextSibling){nl.push(n=x);if(x=n.firstChild)nl.push(n=x);}
else n=n.parentNode;}
return nl;},'following-sibling':function(n){var nl=[];while(n=n.nextSibling)nl.push(n);return nl;},'parent':function(n){return n.parentNode?[n.parentNode]:[];},'preceding':function(n){var nl=[],x,p=n.parentNode;while(n){if(x=n.previousSibling){for(n=x;x=n.lastChild;n=x);nl.unshift(n);}
else if(n=n.parentNode){if(n==p)p=p.parentNode;else nl.unshift(n);}}
return nl;},'preceding-sibling':function(n){var nl=[];while(n=n.previousSibling)nl.unshift(n);return nl;},'self':function(n){return[n];},'document-root':function(n){return[n.ownerDocument||n];}};XPath.axis=function(fn,nt){var r,x,al=arguments.length-1,nl=arguments[al],ap=Array.prototype;for(var i=0,j,l=nl.length;--l>=0;){x=fn.call(this,nl[i++]);if(nt&&x.length)x=ap.filter.call(x,nt,this);for(j=2;j<al&&x.length;x=ap.filter.call(x,arguments[j++],this));r=r?this.union(r,x):x;}
return r||[];};XPath.cache={};function compareDocumentPosition(n){if(this==n)return 0;if(this.nodeType==2&&n.nodeType==2)
return(this.ownerElement&&this.ownerElement==n.ownerElement)?32:0;var l=this.ownerElement||this,r=n.ownerElement||n;if(l.sourceIndex>=0&&r.sourceIndex>=0&&l.contains&&r.contains){return(((l.contains(r)&&16)||(r.contains(l)&&8))|((l.sourceIndex<r.sourceIndex&&4)||(r.sourceIndex<l.sourceIndex&&2)))||1;}
var la=l,ra=r,ld=0,rd=0;while(la=la.parentNode)ld++;while(ra=ra.parentNode)rd++;if(ld>rd){while(ld--!=rd)l=l.parentNode;if(l==r)return 2|8;}
else if(rd>ld){while(rd--!=ld)r=r.parentNode;if(r==l)return 4|16;}
while((la=l.parentNode)!=(ra=r.parentNode))
if(!(l=la)||!(r=ra))return 1;while(l=l.nextSibling)
if(l==r)return 4;return 2;};if(w.Node){var np=w.Node.prototype;if(f||!np.compareDocumentPosition)
np.compareDocumentPosition=compareDocumentPosition;if(f||!np.contains){np.contains=function(n){return Boolean(this.compareDocumentPosition(n)&16);};}}
else
XPath.compareDocumentPosition=compareDocumentPosition;if(f||!w.XPathException){function XPathException(c,m){this.name='XPathException';this.code=c;this.message=m;}
var e=XPathException,p=new Error;p.toString=function(){return this.name+':'+this.message;};e.prototype=p;e.NAMESPACE_ERR=14;e.INVALID_EXPRESSION_ERR=51;e.TYPE_ERR=52;w.XPathException=e;}
if(f||!w.XPathNSResolver){function XPathNSResolver(n){this.ns={};for(var m,a,i=n.attributes.length;--i>=0;)
if(m=/xmlns:(.+)/.exec((a=n.attributes[i]).nodeName))
this.ns[m[1]]=a.nodeValue;this.ns['']=n.getAttribute('targetNamespace');}
XPathNSResolver.prototype={lookupNamespaceURI:function(p){return this.ns[p||''];}};w.XPathNSResolver=XPathNSResolver;}
if(f||!w.XPathExpression){function XPathExpression(e,nsr){this.fn=XPath.cache[e]||(XPath.cache[e]=new XPath(e));this.nsr=nsr;}
XPathExpression.prototype={evaluate:function(n,rt){return new XPathResult(this.fn(n,this.nsr),rt);}};w.XPathExpression=XPathExpression;}
if(f||!w.XPathResult){function XPathResult(r,rt){if(rt==0){switch(typeof r){default:rt++;case'boolean':rt++;case'string':rt++;case'number':rt++;}}
this.resultType=rt;switch(rt){case 1:this.numberValue=XPath.number(r);return;case 2:this.stringValue=XPath.string(r);return;case 3:this.booleanValue=XPath.bool(r);return;case 4:case 5:if(r instanceof Array){this.value=r;this.index=0;this.invalidIteratorState=false;return;}
break;case 6:case 7:if(r instanceof Array){this.value=r;this.snapshotLength=r.length;return;}
break;case 8:case 9:if(r instanceof Array){this.singleNodeValue=r[0];return;}}
throw new XPathException(52);}
var r=XPathResult;r.ANY_TYPE=0;r.NUMBER_TYPE=1;r.STRING_TYPE=2;r.BOOLEAN_TYPE=3;r.UNORDERED_NODE_ITERATOR_TYPE=4;r.ORDERED_NODE_ITERATOR_TYPE=5;r.UNORDERED_NODE_SNAPSHOT_TYPE=6;r.ORDERED_NODE_SNAPSHOT_TYPE=7;r.ANY_UNORDERED_NODE_TYPE=8;r.FIRST_ORDERED_NODE_TYPE=9;r.prototype={iterateNext:function(){switch(this.resultType){case 4:case 5:return this.value[this.index++];}
throw new XPathException(52);},snapshotItem:function(i){switch(this.resultType){case 6:case 7:return this.value[i];}
throw new XPathException(52);}};w.XPathResult=r;}
if(f||!w.XPathEvaluator){function XPathEvaluator(){}
var e=XPathEvaluator;e.prototype={createExpression:function(e,nsr){return new XPathExpression(e,nsr);},createNSResolver:function(n){return new XPathNSResolver(n);},evaluate:function(e,n,nsr,rt){return new XPathExpression(e,nsr).evaluate(n,rt);}};e.install=function(o,f){for(var k in XPathEvaluator.prototype)
if(f||!o[k])o[k]=XPathEvaluator.prototype[k];};w.XPathEvaluator=e;if(w.Document)
e.install(w.Document.prototype,f);else
e.install(w.document,f);w.XPath=XPath;}})(window,/WebKit/.test(navigator.userAgent));