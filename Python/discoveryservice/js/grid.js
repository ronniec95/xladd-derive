"use strict";

// TODO:
// different themes/skins - different node types
// perhaps: include this? and try a demo?   http://layout.jquery-dev.net/lib/js/jquery.layout-latest.js

// 
// TODO: 29/12/2019
// TIDY code --> Mouse stuff together? Key stuff together?
// 
// MOVE all selected together?
// 
// Select via bounding box -> each object - intersect with bounding box ? if so set "hover" true?
// until bounding box released - at which point, select everything in bounding box?

// setting objects as higher or lower? i.e. the drawing order ?
// so -> when we move (drag) things - set them above?
// e.g. this.Nodes = this.Nodes slice and push ??

// tooltip - if hover - after a second or so if not moving - show tooltip ? or just use selected -> to node textarea ??

// Permit other(?) types of nodes - e.g. circle node, or rounded or diff draw styles or colours?
// finish the move of draw styles into Decorator - consider: more css ?? or less?
// save theme - e.g. theme creator/selector ?

// Point2D, Grid, Mouse, Line, DrawArrow, DrawArc, etc.
// Need to retain a stack (push, pop) of commands for ctrl-z ctrl-y support
// Wish to be able to see and add objects, resize, expand/contract subgraphs
// moving an object with connected arcs/lines, also moves the node connectors, and thus the lines/arcs
// scale/pan (scale/translate)
// TODO: revisit pan
// TODO: bounding box on left click and mouse move --> selection based on box?
// TODO: add connector
// TODO: load/save buttons and/or context menu
// TODO: bring to top - can do it by re-ording nodes - using splice
// TODO: events - e.g. interacting with the graph on the web page... maybe need a node js server and set up web service?

// TODO: 27/12/2019: refactor - split classes out and decorator, loader, move mouse events into mouse class(?)
// handle various context menu events
// add graph functionality (dependencies, dependants, check/analyse)
// add _SAVE_ - json, xml, dot formats, and load.. split out loading into graph

var graph;

class GraphPainter {
    
    static drawLine(ctx, pt1, pt2) {

        let right = pt2.X - 8;
        let w = Math.abs(pt1.X - right) / 2.0;
        let w2 = Math.max(60, w);

        ctx.beginPath();
        ctx.moveTo(pt1.X, pt1.Y);
        ctx.arc(pt1.X, pt1.Y, 4, 0, 2 * Math.PI);

        ctx.lineTo(right, pt2.Y - 1.5);
        ctx.lineTo(right, pt2.Y + 1.5);
        ctx.lineTo(pt1.X, pt1.Y);

        ctx.closePath();
        ctx.fillStyle = ctx.strokeStyle;
        ctx.fill();
        ctx.stroke();

        GraphPainter.drawArrowHead(ctx, right, pt2.Y, 0);

    }

    static drawArc(ctx, pt1, pt2) {
        // Draw an arc - bezier - 4 point
        // e.g. https://javascript.info/bezier-curve

        let right = pt2.X - 8;
        let w = Math.abs(pt1.X - right) / 2.0;
        let w2 = Math.max(60, w);

        ctx.beginPath();
        ctx.moveTo(pt1.X, pt1.Y);
        ctx.arc(pt1.X, pt1.Y, 4, 0, 2 * Math.PI);

        ctx.bezierCurveTo(pt1.X + w2, pt1.Y, right - w2, pt2.Y, right, pt2.Y - 1.5);
        ctx.lineTo(right, pt2.Y + 1.5);
        ctx.bezierCurveTo(right - w2, pt2.Y + 1, pt1.X + w2, pt1.Y, pt1.X, pt1.Y);

        ctx.closePath();
        ctx.fillStyle = ctx.strokeStyle;
        ctx.fill();
        ctx.stroke();

        GraphPainter.drawArrowHead(ctx, right, pt2.Y, 0);
    }

    static drawArrowHead(ctx, x, y, extraSize = 0) {

        let offsetX = 2;
        
        ctx.beginPath();
        ctx.moveTo(x + offsetX, y - 5);
        ctx.lineTo(x + 10 + offsetX, y);
        ctx.lineTo(x + offsetX, y + 5);
        //ctx.lineTo(p1.X, p1.Y);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
    }
}

// *** Event stuff

function addNode() {
    // get the text from the textArea
    let text = document.getElementById("nodeDef").value;
    
    // parse the text
    let fns = text.split(';');
    console.log(fns);
    let left = 200;
    let top = 50;
    for (let nodestring of fns) {

        nodestring = nodestring.trim();
        if (nodestring.length > 0) {
            // parse the node text
            let newNode = parseFunctionPrototype(nodestring, left, top);
            // add the node to the graph
            graph.add(newNode);
            
            // cascade
            left += 10;
            top += 10;
        }
    }
    
    graph.draw(ctx);
}

function parseFunctionPrototype(prototype, left = 200, top = 50) {
    
    let id = graph.getNextId();
    
    let i = prototype.indexOf(' ');
    let returnType = prototype.substr(0, i);
    i = i + 1;
    
    let i2 = prototype.indexOf('(', i);
    let nodeName = prototype.substr(i, i2-i);
    i2 = i2 + 1;
    
    let i3 = prototype.indexOf(')', i2);
    let parametersText = prototype.substr(i2, i3 - i2);
    let paramParts = parametersText.split(' ');

    let node = new GraphNode(id, nodeName, left, top);
    let connector;
    
    if (paramParts[0].length > 0)
        for (i = 0; i < paramParts.length / 2; i++) {
            let paramName = paramParts[i*2+1].replace(',', '');
            let paramType = paramParts[i*2];
            connector = new GraphNodeConnector(paramName, true, paramType, node);
            node.addConnector(connector);
        }
    
    if (returnType != "void") {
        connector = new GraphNodeConnector(returnType.replace('System.', ''), false, returnType, node);
        node.addConnector(connector);
    }
    
    return node;
}

function nodeText() {
    
    let nodeString = "";
    for (let n of graph.Nodes) {
        if (n.Selected) {
            
            let so = [];
            let s = "";
            if (n.Outputs.length == 0) {
                s += "void ";
            } else if (n.Outputs.length == 1) {
                s += `${n.Connectors[n.Outputs[0]].Tag} `;
            } else {
                for (let o of n.Outputs) {
                    let oc = n.Connectors[o];
                    so.push(`out ${oc.Tag} ${o}`);
                }
            }

            let si = [];
            for (let i of n.Inputs) {
                let ic = n.Connectors[i];
                si.push(`${ic.Tag} ${i}`);
            }
            
            s += n.Name;
            s += '(';
            if (si.length > 0) {
                s += si.join(", ");
                if (so.length > 0)
                    s += ", ";
            }
            s += so.join(", ");
            s += ');\r';
            
            nodeString += s;
        }
    }
    
    //console.log(nodeString);
    document.getElementById("nodeDef").value = nodeString;
}


// canvas, context and Mouse

//;(function () {
var canvas, ctx, mouse;
var mouseIsDown = false;
var hoverObj = null;
var startEdge = null;
var connectorsAvailable = false;
var selecting = false;
var selectRect;

var canvasFocus = false;
var scale = 1;

function deselectAll() {
    graph.deselectAll();
    graph.draw(ctx);
}

// what about a "no class" type Mouse object?

// helper / visualizer to see x / y coordinates
class Mouse {
    constructor(ctx, x = 0, y = 0) {
        this.x = x;
        this.y = y;
        this.ctx = ctx;
        this.mouseIsDown = false;
        this.downPos = new Point2D(x, y);
    }

    // sets x, y from mousemove event
    set pos(evt) {
        const canvasDimensions = canvas.getBoundingClientRect();

        // get mouse position relative to canvas
        this.x = Math.floor(evt.clientX - canvasDimensions.left) / scale;
        this.y = Math.floor(evt.clientY - canvasDimensions.top) / scale;

        const {
            x,
            y,
            ctx
        } = this;
        
        let x2 = Math.round(x * 100) / 100;
        let y2 = Math.round(y * 100) / 100;
        const txt = `(${x2}, ${y2}) ${Math.round(scale*100)}%`;

        // offset the text position for readability (so it doesnt go off screen)
//        const offsetX = 20; //x < canvas.width / 2 ? 20 : -ctx.measureText(txt).width - 20
//        const offsetY = 20; //y < canvas.height / 2 ? 25 : -18

        if (hoverObj)
            hoverObj.Hover = false;

        let hoverNode = graph.getObjectAtPoint(ctx, x, y);
        if (hoverNode) {

            hoverNode.Hover = true;
            hoverObj = hoverNode;

            if (hoverObj instanceof GraphNode)
                canvas.style.cursor = "move";
            else
                canvas.style.cursor = "pointer";
        } else {
            hoverObj = null;
            canvas.style.cursor = "default";
        }

        if (selecting) {

            // draw some bounding box around the selection
            // todo: if shift is down, then add to selection, else replace selection? or not worry about shift?
            // e.g. if shift is not down, then simply reselect ??
            // we want all the objects within the selection range...

            let left = Math.min(mouse.downPos.X, mouse.x);
            let top = Math.min(mouse.downPos.Y, mouse.y);
            let width = Math.abs(mouse.downPos.X - mouse.x);
            let height = Math.abs(mouse.downPos.Y - mouse.y);

            selectRect = { Left: left, Top: top, Width: width, Height: height, Right: left + width, Bottom: top + height };

            // set hover objects by selection
            // todo: set hover false
            graph.dehoverAll();
            let intersecting = graph.getObjectsIntersecting(selectRect);
            for (let io of intersecting)
                io.Hover = true;
        }
        
        // draw first, then - draw extra edges etc, however, if we've just hovered over something new... we should draw..
        graph.draw(ctx);

        if (selecting) {
            ctx.save();

            ctx.scale(graph.scale, graph.scale);
            ctx.fillStyle = "rgba(0,0,0,0.15)";
            ctx.fillRect(selectRect.Left, selectRect.Top, selectRect.Width, selectRect.Height);

            ctx.restore();
            
        }
        
        // If we are doing a dragging operation from a (valid) connector
        if (this.mouseIsDown) {

            if (startEdge) {

                ctx.save();

                ctx.scale(graph.scale, graph.scale);
                
                // TODO: refactor arc drawing
                // TODO: Decorator arc stuff
                ctx.shadowOffsetX = 1;
                ctx.shadowOffsetY = 1;
                ctx.shadowColor = "#888";
                ctx.shadowBlur = 3;

                ctx.fillStyle = "#333"; // "#687cc4";// "#acb8e3";// "#cf793c"; //"#687cc4";
                ctx.lineWidth = 1;

                // are there connectors available? if so - make this green, else red?
                ctx.strokeStyle = connectorsAvailable ? "LawnGreen" : "#fc0400"; //"#d9691a";

                // test if we are near an available connector - if we are - do some snapping and highlight the arrowhead?
                // also - instead of this -- arc decoration code above - use the draw edge ?

                GraphPainter.drawArc(ctx, mouse.downPos, new Point2D(x, y));

                ctx.restore();
            }
        }

        //var l = canvasDimensions.width - 200;
        // set the font
        ctx.font = '10px Monospace'
        ctx.fillText(txt, 20, 390);

    }

}

// Mouse dragging: todo: into mouse class ?

var dragging;
var dragX = 0;
var dragY = 0;

function doMouseDown(event, mouse) {

    if (menuVisible)
        toggleMenu("hide");

    //cvx = event.pageX;
    //      if (!mouse.mouseIsDown)
    //          console.log('mousing down');
    if (event.button == 0)
        mouse.mouseIsDown = true;

    mouse.downPos = new Point2D(mouse.x, mouse.y);

    let hoverNode = graph.getObjectAtPoint(ctx, mouse.downPos.X, mouse.downPos.Y);
    //console.log(graph, hoverNode, mouse.downPos.X, mouse.downPos.Y);
    if (hoverNode) {

        if (event.button == 0) {
            dragging = hoverNode;
            dragX = mouse.downPos.X;
            dragY = mouse.downPos.Y;
            hoverNode.Selected = !hoverNode.Selected;

            if (hoverNode instanceof GraphNode) {
                //hoverNode.draw(ctx);
                graph.draw(ctx);
            }

            if (hoverNode instanceof GraphEdge) {
                //hoverNode.draw(ctx, graph);
                graph.draw(ctx);
            }

            if (hoverNode instanceof GraphNodeConnector) {

                // can we start drawing an arc ?
                if (!hoverNode.Input) {
                    let n = hoverNode.Node;
                    let p = hoverNode.Position;
                    startEdge = hoverNode;
                    mouse.downPos = new Point2D(n.Left + p.X, n.Top + p.Y);

                    // now need to highlight available nodes...
                    let i;
                    let available = graph.getAvailableConnectors(hoverNode.Tag);
                    //console.log("available=", available.length);
                    for (i = 0; i < available.length; i++) {
                        available[i].Available = true;
                    }

                    connectorsAvailable = available.length > 0;
                }

            }

            return;
        }

        if (event.button == 2) {

            //console.log("context menu on ", hoverNode);
            
            event.preventDefault();
            
            // set context menu position
            const origin = {
                left: event.clientX + window.scrollX,
                top: event.clientY + window.scrollY
            };
            setPosition(origin);

            setupContextMenu(hoverNode);

            // only prevent default in certain situations.. which comes first?
            //console.log("1");
        }

        //graph.draw(ctx);
    }
    else if (event.button == 0) {
        selecting = true;
    }
}

function doMouseUp(event, mouse) {
    mouse.mouseIsDown = false;

    dragging = null;

    if (startEdge) {
        // TODO: check to see if we finished at an available node... if we did - add a new edge to the graph
        let objAtMouse = graph.getObjectAtPoint(ctx, mouse.x, mouse.y);
        if (objAtMouse) {
            //console.log("mouse up at: ", mouse.x, mouse.y, objAtMouse);
            if (objAtMouse instanceof GraphNodeConnector) {
                if (objAtMouse.Available) {
                    // Todo: add graph edge from original connector (startEdge) to objAtMouse
                    //console.log(startEdge.Node.Id, objAtMouse.Node.Id, startEdge.Name, objAtMouse.Name);
                    
                    let edge = new GraphEdge(startEdge.Node.Id, objAtMouse.Node.Id, startEdge.Name, objAtMouse.Name);
                    graph.add(edge);
                }
            }
        }
        
        graph.setNoAvailableConnectors();
    }

    if (selecting) {
        // todo - each object - intersectsWithRect ?
        graph.dehoverAll();
        let intersecting = graph.getObjectsIntersecting(selectRect);
        for (let io of intersecting)
            io.Selected = true;
        selectRect = null;
    }
    
    selecting = false;
    startEdge = null;
}

function doMouseMove(event, mouse) {
    mouse.pos = event;
    
    if (dragging) {
        
        // TODO: this is updating the position of 'dragging' - which is a Node
        if (!dragging instanceof GraphNode) {
            console.log("do something about this");
        }
        
        dragging.Left += mouse.x - dragX;
        dragging.Top += mouse.y - dragY;
        dragX = mouse.x;
        dragY = mouse.y;
    }
}

// Initialisation
// The canvas object needs to redraw as things change - knowing what objects to draw
// thus we create a wrapper object, and add things to it
// in our case, the wrapper object is the graph (see loadGraphFromXml) and on various events we
// update some object(s) and call graph.draw(ctx) - supplying the (canvas) context in which to draw
// the graph.draw function draws nodes and edges etc, based on their states

function init() {

    // set our config variables
    canvas = document.getElementById('canvasBase')
    ctx = canvas.getContext('2d')
    mouse = new Mouse(ctx)

    canvas.addEventListener("mousemove", function () { doMouseMove(event, mouse); }, false);
    canvas.addEventListener("mousedown", function () { doMouseDown(event, mouse); }, false);
    canvas.addEventListener("mouseup", function () { doMouseUp(event, mouse); }, false);
    
    canvas.addEventListener ("mouseenter", function () { canvasFocus = true; canvas.style.boxShadow = "0 0 8px 1px #b37b49 inset"; }, false);
    canvas.addEventListener ("mouseout", function () { canvasFocus = false; canvas.style.boxShadow = "0 0 8px 1px #f5c193 inset"; }, false);

    //init2();

    graph = new AarcGraph();
}

function init2() {
    //var svgel = d3.select("body").append("svg").attr("width", 500).attr("height", 500);
    //var svgel = d3.select("canvasBase");
    //var circle = svgel.append("circle").attr("cx", 50).attr("cy", 50).attr("r", 25);

    //    var data = [];
    //    var value = 200;
    //    var colourScale;        
    //    var canvasD3 = d3.select("body").append("canvas").attr("width", 500).attr("height", 500);
    //    d3.range(value).forEach(function(el) {
    //        data.push({ value: el });
    //    });

    //window.onload= start(graph);

    // so this is the "draw canvas" method - all the objects of the graph
    var ctx = document.getElementById("canvasBase").getContext("2d");

    try {
        let fn = "ChartTickerDistribution.xml"; // try: Simplified_Sims.xml or ChartTickerDistribution.xml

        // the '2019' way... apparently
        fetch(fn)
          .then(response => response.text())
          .then((data) => {
            loadGraphFromXml(data)
          });
    }
    catch(err) {
        console.log(err);
    }

    // listeners for keydown
    d3.select('body').on('keydown', function () {

        if (canvasFocus)
            console.log(d3.event.keyCode);

        // http://gcctech.org/csc/javascript/javascript_keycodes.htm
        switch (d3.event.keyCode) {
            case 46: // delete
                // only want to do this when the canvas has focus!
                if (canvasFocus) {
                    graph.deleteSelected();
                    graph.draw(ctx);
                }
                break;

            case 16: // shift
                break;

            case 17: // ctrl
                break;
        }

    }); // text input listener/handler

//    document.addEventListener('readystatechange', event => {
//
//        if (event.target.readyState === "interactive") { //same as:  ..addEventListener("DOMContentLoaded".. and   jQuery.ready
//            console.log("All HTML DOM elements are accessible");
//        }
//
//        if (event.target.readyState === "complete") {
//            console.log("Now external resources are loaded too, like css,src etc... ");
//        }
//
//    });

    var lastPosX = 0;
    var lastPosY = 0;
    var newPosX = 0;
    var newPosY = 0;

    var mouseDown = 0;
    document.body.onmousedown = function () {
        ++mouseDown;
    }
    document.body.onmouseup = function () {
        --mouseDown;
    }

    // TODO: Problems with zoom and pan: although they "work" - subsequently throws off the mouse positioning
    // which needs to handle the transformations

    // something like this anyway...
    window.onmousemove = function (e) {
        if (!e) e = window.event;
        if (e.shiftKey) {
            /*shift is down*/

            lastPosX = newPosX == 0 ? e.clientX : newPosX;
            lastPosY = newPosY == 0 ? e.clientY : newPosY;

            newPosX = e.clientX;
            newPosY = e.clientY;

            // TODO: potentially better idea for pan: actually change the coords of nodes so that 
            // when we subsequently save and load - the positions are as they were saved
            // i.e. instead of translate. code is far simpler too - just move each node in graph??
            //pan(e);
        }
    }

    window.onwheel = function (e) {
        //console.log(e);

        if (!e) e = window.event;
        if (e.shiftKey) {
            /*shift is down*/
            //e.preventDefault();
            zoom(e);
        }
        if (e.altKey) {
            /*alt is down*/
        }
        if (e.ctrlKey) {
            /*ctrl is down*/
        }
        if (e.metaKey) {
            /*cmd is down*/
        }
    }

    // TODO: snap - e.g. https://konvajs.org/docs/sandbox/Animals_on_the_Beach_Game.html
    // isNearOutline ...

    // TODO: pan with - how? 
    // shift-left mouse to pan (if not on hover, then change cursor on shift press)
    // shift wheel to zoom
    function pan(event) {

        if (mouseDown) {

            graph.offsetX += newPosX - lastPosX;
            graph.offsetY += newPosY - lastPosY;

        }
    }

    function zoom(event) {
        //event.preventDefault();

        let ds = event.deltaY > 0 ? -0.05 : 0.05;
        scale += ds; //event.deltaY * -0.001;

        // Restrict scale
        scale = Math.min(Math.max(.2, scale), 3);

        // Apply scale transform
        //console.log(event.deltaY, scale, ctx);
        graph.scale = scale;
        //ctx.scale(scale, scale);

        graph.draw(ctx);
    }

}

function loadGraphFromXml(xmlText) {

    graph = GraphSerializer.fromXml(xmlText);
    graph.draw(ctx);
}

function loadFromFile(file) {

    // setting up the reader
    var reader = new FileReader();
    reader.readAsText(file, 'UTF-8');

    // here we tell the reader what to do when it's done reading...
    reader.onload = readerEvent => {
        var content = readerEvent.target.result; // this is the content!
        try {
            loadGraphFromXml(content);
        } catch(err) {
            alert("Unable to load graph from " + file);
        }
    }
}

function loadXml() {
    //document.getElementById('file-input').click();

    var input = document.createElement('input');
    input.type = 'file';

    input.onchange = e => {

        /*
            file.name // the file's name including extension
            file.size // the size in bytes
            file.type // file type ex. 'application/pdf'    
        */

        // TODO: check it's valid xml etc?
        var file = e.target.files[0];

        loadFromFile(file);
    }

    input.click();
}

function saveXml() {

    let xmlDoc = GraphSerializer.toXmlDoc(graph);
    let s = prettifyXml(xmlDoc);

    //console.log(s);
  
    //var FileSaver = require("FileSaver");
    var blob = new Blob([s], {type: "text/plain;charset=utf-8"});
    saveAs(blob, "AarcGraph.xml");
}

function prettifyXml(xmlDoc) {
    //var xmlDoc = new DOMParser().parseFromString(sourceXml, 'application/xml');
    var xsltDoc = new DOMParser().parseFromString([
        // describes how we want to modify the XML - indent everything
        '<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform">',
        '  <xsl:strip-space elements="*"/>',
        '  <xsl:template match="para[content-style][not(text())]">', // change to just text() to strip space in text nodes
        '    <xsl:value-of select="normalize-space(.)"/>',
        '  </xsl:template>',
        '  <xsl:template match="node()|@*">',
        '    <xsl:copy><xsl:apply-templates select="node()|@*"/></xsl:copy>',
        '  </xsl:template>',
        '  <xsl:output indent="yes"/>',
        '</xsl:stylesheet>',
    ].join('\n'), 'application/xml');

    var xsltProcessor = new XSLTProcessor();    
    xsltProcessor.importStylesheet(xsltDoc);
    var resultDoc = xsltProcessor.transformToDocument(xmlDoc);
    var resultXml = new XMLSerializer().serializeToString(resultDoc);
    return resultXml;
}


// Different radii for each corner, others default to 0
//roundRect(ctx, 10, 100, 200, 100, { tr: 10, br: 10 }, true);

/**
 * from https://stackoverflow.com/questions/1255512/how-to-draw-a-rounded-rectangle-on-html-canvas
 * Draws a rounded rectangle using the current state of the canvas.
 * If you omit the last three params, it will draw a rectangle
 * outline with a 5 pixel border radius
 * @param {CanvasRenderingContext2D} ctx
 * @param {Number} x The top left x coordinate
 * @param {Number} y The top left y coordinate
 * @param {Number} width The width of the rectangle
 * @param {Number} height The height of the rectangle
 * @param {Number} [radius = 5] The corner radius; It can also be an object 
 *                 to specify different radii for corners
 * @param {Number} [radius.tl = 0] Top left
 * @param {Number} [radius.tr = 0] Top right
 * @param {Number} [radius.br = 0] Bottom right
 * @param {Number} [radius.bl = 0] Bottom left
 * @param {Boolean} [fill = false] Whether to fill the rectangle.
 * @param {Boolean} [stroke = true] Whether to stroke the rectangle.
 */
function roundRect(ctx, x, y, width, height, radius, fill, stroke) {
    if (typeof stroke === 'undefined') {
        stroke = true;
    }
    if (typeof radius === 'undefined') {
        radius = 5;
    }
    if (typeof radius === 'number') {
        radius = {
            tl: radius,
            tr: radius,
            br: radius,
            bl: radius
        };
    } else {
        var defaultRadius = {
            tl: 0,
            tr: 0,
            br: 0,
            bl: 0
        };
        for (var side in defaultRadius) {
            radius[side] = radius[side] || defaultRadius[side];
        }
    }
    ctx.beginPath();
    ctx.moveTo(x + radius.tl, y);
    ctx.lineTo(x + width - radius.tr, y);
    ctx.quadraticCurveTo(x + width, y, x + width, y + radius.tr);
    ctx.lineTo(x + width, y + height - radius.br);
    ctx.quadraticCurveTo(x + width, y + height, x + width - radius.br, y + height);
    ctx.lineTo(x + radius.bl, y + height);
    ctx.quadraticCurveTo(x, y + height, x, y + height - radius.bl);
    ctx.lineTo(x, y + radius.tl);
    ctx.quadraticCurveTo(x, y, x + radius.tl, y);
    ctx.closePath();
    if (fill) {
        ctx.fill();
    }
    if (stroke) {
        ctx.stroke();
    }
}

