"use strict";

// https://github.com/openfl/openfl/issues/973
//untyped __js__('// openfl IE/Edge isPointInStroke shiv - not super exact for wide strokes
if(typeof CanvasRenderingContext2D.prototype.isPointInStroke == "undefined"){
    console.log("Pathed CanvasRenderingContext2D with isPointInStroke approximation");

    CanvasRenderingContext2D.prototype.isPointInStroke = function(path, x, y) {
        return this.isPointInPath(x, y);
    }
}//');

class Point2D {
    constructor(x = 0, y = 0) {
        this.X = x;
        this.Y = y;
    }
}

/*

start with Title: always get the 

rect:
 - stroke
 - fill (e.g. gradient)
 - shadow

 - title (fillText, stroke, font, shadow)

 - separator (lineWidth, strokeStyle, shadow)

 - connector (stroke, lineWidth, fillStyle)
 - connector text (stroke, font) - uses ctx.fillText

*/

class GraphSerializer {

    static fromXml(xmlText) {

        let xmlDoc;
        if (window.DOMParser)
        {
            let parser = new DOMParser();
            xmlDoc = parser.parseFromString(xmlText, "text/xml");
        }
        else // Internet Explorer
        {
            xmlDoc = new ActiveXObject("Microsoft.XMLDOM");
            xmlDoc.async = false;
            xmlDoc.loadXML(xmlText);
        }

        let g = new AarcGraph();

        let nodes = xmlDoc.getElementsByTagName("Node");
        //console.log("nodes=", nodes);

        // for each node in nodes
        console.log("Nodes: ");
        let i;
        for (i = 0; i < nodes.length; i++) {

            let n = nodes[i];
            //console.log(n.attributes);

            var title = n.attributes["Title"].nodeValue;
            var id = parseInt(n.attributes["Id"].nodeValue);
            console.log(` ${i}:`, title, id);

            let location = n.getElementsByTagName("Location")[0].attributes;
            let nodeX = parseInt(location.X.nodeValue);
            let nodeY = parseInt(location.Y.nodeValue);
            //console.log(" x=", nodeX, " y=", nodeY);

            let items = n.getElementsByTagName("Item");

            let node = new GraphNode(id, title, nodeX * 1.0, nodeY);

            // create the graph node connectors
            let j;
            for (j = 0; j < items.length; j++) {
                let item = items[j].attributes;
                //console.log("  ", item.Text.nodeValue, " in=", item.In.nodeValue, `tag=${item.Tag.nodeValue}`);
                var connector = new GraphNodeConnector(item.Text.nodeValue, item.In.nodeValue === "True", item.Tag.nodeValue, node);
                node.addConnector(connector);
            }

            g.add(node);
        }

        console.log("Edges: ");
        let edges = xmlDoc.getElementsByTagName("Connection");
        for (i = 0; i < edges.length; i++) {
            let e = edges[i].attributes;
            var sourceId = parseInt(e["SourceId"].nodeValue);
            var targetId = parseInt(e["TargetId"].nodeValue);
            var edgeOut = e["Out"].nodeValue;
            var edgeIn = e["In"].nodeValue;

            console.log(` ${i}:`, sourceId, targetId, edgeOut, edgeIn);
            var edge = new GraphEdge(sourceId, targetId, edgeOut, edgeIn);

            g.add(edge);
        }

        return g;
    }
    
    static toXmlDoc(graph) {

        let xmlDoc = document.implementation.createDocument("", "", null);
        let rootEl = xmlDoc.createElement("AarcGraph");
        rootEl.setAttribute("NodeCount", graph.Nodes.length);
        xmlDoc.appendChild(rootEl);
        
        let nodesElem = xmlDoc.createElement("Nodes");
        for (let n of graph.Nodes) {
            let nodeEl = xmlDoc.createElement("Node");
            nodeEl.setAttribute("Id", n.Id);
            nodeEl.setAttribute("Title", n.Name);
            
            let locEl = xmlDoc.createElement("Location");
            locEl.setAttribute("X", n.Left);
            locEl.setAttribute("Y", n.Top);
            
            nodeEl.appendChild(locEl);

            for (let cName in n.Connectors) {
                let c = n.Connectors[cName];
                let itemEl = xmlDoc.createElement("Item");
                itemEl.setAttribute("Text", cName);
                itemEl.setAttribute("In", c.Input == true ? "True" : "False");
                itemEl.setAttribute("Out", c.Input == true ? "True" : "False");
                itemEl.setAttribute("Tag", c.Tag);
                
                nodeEl.appendChild(itemEl);
            }
            
            nodesElem.appendChild(nodeEl);
        }
        
        let edgesElem = xmlDoc.createElement("Connections");
        for (let e of graph.Edges) {
            let connEl = xmlDoc.createElement("Connection");
            connEl.setAttribute("SourceId", e.SourceId);
            connEl.setAttribute("TargetId", e.TargetId);
            connEl.setAttribute("Out", e.EdgeOut);
            connEl.setAttribute("In", e.EdgeIn);
            
            edgesElem.appendChild(connEl);
        }

        rootEl.appendChild(nodesElem);
        rootEl.appendChild(edgesElem);
        
        return xmlDoc;
    }
    
}

// TODO: separate graph logic from draing logic by e.g. injecting a GraphPainter ?
// graph consists of nodes and edges
class AarcGraph {

    constructor() {
        this.Nodes = [];
        this.Edges = [];
        
        this.offsetX = 0;
        this.offsetY = 0;
        
        this.dx = 0;
        this.dy = 0;
        
        this.scale = 1;
    }

    add(obj) {
        // check that it's a drawable
        if (obj !== null && typeof obj === 'object')
        {
            if (obj instanceof GraphNode) {
                this.Nodes.push(obj);
                //console.log("Node.add succeeded. length=" + this.Nodes.length);
            }
            else if (obj instanceof GraphEdge) {
                this.Edges.push(obj);
                //console.log("Edge.add succeeded. length=" + this.Edges.length);
            }
        }
    }

    getNextId() {

        if (this.Nodes.length == 0)
            return 1;
        
        let ids = [];
        for (let n of this.Nodes) {
            ids.push(n.Id);
        }

        return Math.max(...ids) + 1;
    }

    dehoverAll() {
        for (let n of graph.Nodes)
            n.Hover = false;
        for (let e of graph.Edges)
            e.Hover = false;
    }
    
    deselectAll() {
        for (let n of this.Nodes)
            n.Selected = false;
        for (let e of this.Edges)
            e.Selected = false;
    }
    
    deleteSelected() {
        let i;
        let nodesToDelete = [];
        
        for (i = 0; i < this.Nodes.length; i++) {
            if (this.Nodes[i].Selected) {
                //console.log("delete node:", this.Nodes[i].Name, this.Nodes[i].Id);
                nodesToDelete.push({ Name: this.Nodes[i].Name, Id: this.Nodes[i].Id });
            }
            
            // todo: and need to delete all edges that go to this node!
        }

        let edgesToKeep = [];
        
        for (i = 0; i < this.Edges.length; i++) {

            //this.SourceId = sourceId;
            //this.TargetId = targetId;
            
            let deleteThisEdge = false;

            let j;
            for (j = 0; j < nodesToDelete.length; j++) {
                if (this.Edges[i].SourceId == nodesToDelete[j].Id)
                    deleteThisEdge = true;
                if (this.Edges[i].TargetId == nodesToDelete[j].Id)
                    deleteThisEdge = true;
            }
            
            if (this.Edges[i].Selected) {
                deleteThisEdge = true;
            }
            
            if (!deleteThisEdge)
                edgesToKeep.push(this.Edges[i]);
        }

        this.Edges = edgesToKeep;
        this.Nodes = this.Nodes.filter(function(value, index, arr){
            return value.Selected != true;
        });
    }
    
    deleteNode(node) {
        this.Nodes = this.Nodes.filter(function(n) {
            return !GraphNode.areEqual(node, n);
        });
        
        // delete edges for this node
        this.Edges = this.Edges.filter(function(e) {
            return e.SourceId != node.Id && e.TargetId != node.Id;
        });
    }

    deleteEdge(edge) {
        this.Edges = this.Edges.filter(function(e) {
            return !GraphEdge.areEqual(edge, e);
        });
    }
    
    removeNode(name) {
        this.Nodes = this.Nodes.filter(function(value, index, arr){
            return value.Name != name;
        });
    }

    // get any edges connected to this object (either node, or more specific: connector)
    getEdges(obj) {
        if (obj instanceof GraphNodeConnector) {
            let edges = this.Edges.filter(function(e) {
                // todo: edge is connected to this connector function?
                let connector = obj;
                if (connector.Input && e.EdgeIn == connector.Name && e.TargetId == connector.Node.Id)
                    return true;
                if (!connector.Input && e.EdgeOut == connector.Name && e.SourceId == connector.Node.Id)
                    return true;
                return false;
            });
            //console.log("Get edges", edges);
            return edges;
        } else if (obj instanceof GraphNode) {
            let edges = this.Edges.filter(function(e) {
                return (e.TargetId == obj.Id || e.SourceId == obj.Id);
            });
            return edges;
        }
        //console.log("Get edges empty");
        return [];
    }

    // = get parents. recursive. returns ids of nodes which are dependencies (parents) of this node id
    getDependencyNodeIds(nodeId) {
        // it's all about the edges in -> ie where TargetId = node.Id -> getting the source ids gives the result needed
        // and we can make it non-recursive by using an accumulator?
        let deps = [];
        
        // incoming edges
        let edges = this.Edges.filter(function(e) {
            return e.TargetId == nodeId;
        });

        for (let e of edges) {
            if (!deps.includes(e.SourceId))
                deps.push(e.SourceId);

            // and recursively add the dependencies of this nodeId
            for (let d of this.getDependencyNodeIds(e.SourceId)) {
                if (!deps.includes(d))
                    deps.push(d);
            }
        }
        
        return deps;
    }

    // = get children, recursive. returns ids of nodes
    getDependantNodeIds(nodeId) {
        let deps = [];
        
        // incoming edges
        let edges = this.Edges.filter(function(e) {
            return e.SourceId == nodeId;
        });

        for (let e of edges) {
            if (!deps.includes(e.TargetId))
                deps.push(e.TargetId);

            // and recursively add the dependencies of this nodeId
            for (let d of this.getDependantNodeIds(e.TargetId)) {
                if (!deps.includes(d))
                    deps.push(d);
            }
        }
        
        return deps;
    }
    
    // todo: not very efficient especially if many many nodes? would a map be better?
    getNode(nodeId) {
        let i;
        for (i = 0; i < this.Nodes.length; i++) {
            let n = this.Nodes[i];
            if (n.Id === nodeId)
                return n;
        }
        
        throw "No node with id=" + nodeId;
    }

    setNoAvailableConnectors() {

        let i;
        for (i = 0; i < this.Nodes.length; i++) {
            let n = this.Nodes[i];
            
            // n.Connectors.filter on type -> if not connected - add connector
            for (var name in n.Connectors) {
                let c = n.Connectors[name];
                c.Available = false;
            }
        }
        
    }

    isConnected(connector) {

        //console.log("IsConnected: connector", connector.Name, connector.Node.Name, connector.Node.Id);
        // look through edges - is connected if input && incoming edge OR output && outgoing edge ?
        let i;
        for (i = 0; i < this.Edges.length; i++) {
            let e = this.Edges[i];
            // connector is connected if 
            // 1. there is an edge in which corresponds to the connector's name
            // 2. the targetId of the edge corresponds to the connector's node's Id
            if (connector.Input && e.EdgeIn == connector.Name && e.TargetId == connector.Node.Id)
                return true;
        }
        
        return false;
    }
    
    getAvailableConnectors(tag) {
        // some type info is required?
        let available = [];
        
        let i;
        for (i = 0; i < this.Nodes.length; i++) {
            let n = this.Nodes[i];
            
            // n.Connectors.filter on type -> if not connected - add connector
            for (var name in n.Connectors) {
                let c = n.Connectors[name];
                //console.log("node:", n.Name, c.Tag);
                // TODO: whether or not it has an input - or IsConnected
                if (c.Tag == tag && c.Input) {
                    
                    if (!this.isConnected(c))
                        available.push(c);
                }
            }
        }

        return available;
    }

    getObjectsIntersecting(rc) {
        let o = [];
        
        for (let n of this.Nodes) {
            if (n.intersectsWithRect(rc))
                o.push(n);
        }
        
        for (let e of this.Edges) {
            if (e.intersectsWithRect(rc))
                o.push(e);
        }

        return o;
    }
    
    getObjectAtPoint(ctx, x, y) {

        let i;
        // go backwards because nodes are drawn over each other
        for (i = this.Nodes.length - 1; i >= 0; i--) {
            let n = this.Nodes[i];
            
            let f = n.isPointInObject(ctx, x, y);
            if (f)
                return f;
        }
        
        // check edges
        for (i = 0; i < this.Edges.length; i++) {
            let e = this.Edges[i];
            e.getPath(ctx);
            if (ctx.isPointInPath(x, y) || ctx.isPointInStroke(x, y))
                return e;
        }
    }
    
    draw(ctx) {

        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        ctx.save();

        // scale then translate, or translate then scale?
        //ctx.translate(this.offsetX, this.offsetY);
        ctx.scale(this.scale, this.scale);
        
        // Draw Nodes
        var i;
        for (i = 0; i < this.Nodes.length; i++) {
            this.Nodes[i].draw(ctx);
        }

        // Draw Edges TODO: Decorator arc stuff
        GraphDecorator.setStyle(ctx, "edgeShadow");
        ctx.fillStyle = "#222"; // "#687cc4";// "#acb8e3";// "#cf793c"; //"#687cc4";
        ctx.lineWidth = 1.5;
        ctx.strokeStyle = "#1b2340";// "#4a5d9e"; // "#000";
        
        for (i = 0; i < this.Edges.length; i++) {
            this.Edges[i].draw(ctx, this);
        }
        
        ctx.restore();
    }
}

class GraphEdge {

    // ids of nodes, edgeIn is what? name of connection
    constructor(sourceId, targetId, edgeOut, edgeIn) {

        this.SourceId = sourceId;
        this.TargetId = targetId;
        this.EdgeOut = edgeOut;
        this.EdgeIn = edgeIn;
        
        this.Hover = false;
    }

    static areEqual(e1, e2) {
        return e1.SourceId == e2.SourceId && 
            e1.TargetId == e2.TargetId && 
            e1.EdgeOut == e2.EdgeOut &&
            e1.EdgeIn == e2.EdgeIn;
    }
    
    draw(ctx, graph) {
        
        // need to find the node's position - and thus connector positions - which in turn depends upon the draw methods ?

        var from = graph.getNode(this.SourceId).getConnectorPosition(this.EdgeOut);
        var to = graph.getNode(this.TargetId).getConnectorPosition(this.EdgeIn);

        var ts = ctx.strokeStyle;
        let tw = ctx.lineWidth;
        
        if (this.Selected) {
            ctx.strokeStyle = GraphDecorator.getStyle("selected");
            ctx.lineWidth = 3;
            GraphPainter.drawArc(ctx, from, to);
        }

        //else hover overrides selected.. 
        if (this.Hover) {
            ctx.strokeStyle = GraphDecorator.getStyle("hover");
            ctx.lineWidth = 2;//this.Selected ? 3 : 2;
        }
        
//        let gradient = Math.abs((from.Y - to.Y) / (from.X - to.X));
//        if (gradient < 0.02)
//            GraphPainter.drawLine(ctx, from, to);
//        else
            GraphPainter.drawArc(ctx, from, to);
        
        ctx.strokeStyle = ts;
        ctx.lineWidth = tw;
    }

    intersectsWithRect(rc) {
        return false;
    }
    
    getPath(ctx) {
        
        let pt1 = graph.getNode(this.SourceId).getConnectorPosition(this.EdgeOut);
        let pt2 = graph.getNode(this.TargetId).getConnectorPosition(this.EdgeIn);

        let right = pt2.X - 8;
        let w = Math.abs(pt1.X - right) / 2.0;
        let w2 = Math.max(100, w);

        ctx.beginPath();
        ctx.moveTo(pt1.X, pt1.Y-2);
        ctx.arc(pt1.X, pt1.Y, 4, 0, 2 * Math.PI);
        ctx.bezierCurveTo(pt1.X + w2, pt1.Y, right - w2, pt2.Y, right, pt2.Y - 1.5);
        ctx.lineTo(right, pt2.Y + 1.5);
        ctx.bezierCurveTo(right - w2, pt2.Y + 1, pt1.X + w2, pt1.Y, pt1.X, pt1.Y+2);
        ctx.closePath();
    }
}

// will depend upon connectors going in?
class GraphNodeConnector {
    constructor(name, input, tag, node, left = 0, top = 0) {
        this.Hover = false;
        this.Available = false;

        // Default constructor
        if (name===undefined)
        {
            this.Position = new Point2D(0, 0);
        }
        else
        {
            this.Name = name;
            this.Input = input;
            this.Tag = tag;
            this.Node = node;
            this.Position = new Point2D(left, top);
        }
    }

    static fromJson(json, node)
    {
        let connector = Object.assign(new GraphNodeConnector(), json)
        connector.Node = node;
        return connector;
    }
    draw(ctx) {
    //drawConnector(ctx, connector, node) {
        var textInset = 12;

        // for output:
        let txt = this.Name;
        let textLeft = this.Node.Left + this.Position.X;
        
        if (!this.Input) {
            var w = ctx.measureText(txt).width;
            textLeft = textLeft - w - textInset;
        }
        else {
            textLeft = textLeft + textInset;
        }

        // 4 because it's half the size of the connector?
        ctx.fillText(txt, textLeft, this.Node.Top + this.Position.Y + 4);

        //drawConnectorTriangle(ctx, node.Left + node.Width, connectorTop);
        let highlight = null;
        if (this.Hover)
            highlight = GraphDecorator.getStyle("hover");
        if (this.Available)
            highlight = "LawnGreen";
        
        //this.drawConnectorDot(ctx, this.Node.Left + this.Position.X, this.Node.Top + this.Position.Y, highlight);
        if (!this.Input)
            this.drawConnectorDot(ctx, this.Node.Left + this.Position.X, this.Node.Top + this.Position.Y, highlight);
        else
            this.drawConnectorTriangle(ctx, this.Node.Left + this.Position.X - 1, this.Node.Top + this.Position.Y, highlight);
    }

    isPointInObject(ctx, x, y) {

        ctx.beginPath();
        ctx.arc(this.Node.Left + this.Position.X, this.Node.Top + this.Position.Y, 7, 0, 2 * Math.PI);
        ctx.closePath();
        // check if we hover it, fill red, if not fill it blue
        if (ctx.isPointInPath(x, y))
            return this;
    }
    
    drawConnectorDot(ctx, left, top, highlight = null) {

        ctx.save();

        // TOOD: decorator: connector shadow choices 
        // connector shadow
        ctx.shadowOffsetX = 1;
        ctx.shadowOffsetY = 1;
        ctx.shadowColor = "rgba(0,0,0,0.3)";
        ctx.shadowBlur = 4;

        // draw connectors
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(left, top, 6, 0, 2 * Math.PI);
        //ctx.arc(10, 50, 6, 1.5 * Math.PI, 0.5 * Math.PI);
        ctx.strokeStyle = "#222";
        ctx.stroke();

        // fill the connector
        var gradient = ctx.createRadialGradient(left, top, 0, left - 3, top - 2, 8);
        gradient.addColorStop(0, '#aaa');
        gradient.addColorStop(1, 'white');

        ctx.fillStyle = gradient;
        ctx.fill();

        // for highlighted connector, something like:
        if (highlight) {
            ctx.lineWidth = 2;
            ctx.strokeStyle = highlight;// "#889ce4"; // "#0a0";
            ctx.beginPath();
            ctx.arc(left, top, 8, 0 * Math.PI, 2 * Math.PI);
            ctx.stroke();
        }

        ctx.restore();
    }

    drawConnectorTriangle(ctx, left, top, highlight = null) {

        ctx.save();
        
        // connector shadow
        ctx.shadowOffsetX = 1;
        ctx.shadowOffsetY = 1;
        ctx.shadowColor = "rgba(0,0,0,0.3)";
        ctx.shadowBlur = 4;
    
        // draw connectors
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(left - 2, top - 6);
        ctx.lineTo(left + 8, top);
        ctx.lineTo(left - 2, top + 6);
        ctx.closePath();
    
        // the outline
        ctx.lineWidth = 1;
        ctx.strokeStyle = '#222';
        ctx.stroke();
        ctx.fill();
        
        if (highlight) {
            ctx.lineWidth = 2;
            ctx.strokeStyle = highlight;// "#889ce4"; // "#0a0";
            ctx.beginPath();
            ctx.moveTo(left - 5, top - 8);
            ctx.lineTo(left + 9, top);
            ctx.lineTo(left - 5, top + 8);
            ctx.closePath();
            ctx.stroke();
        }
        
        ctx.restore();
    }
    
}

// drawing a node - wwe need a decorator - or css styles
class GraphNode {

    constructor(id, name, x, y, inputs = [], outputs = []) {

        this.Hover = false;
        this.Selected = false;
        // connector map
        this.Connectors = {};

        // Empty constructor
        if (id==undefined)
        {
            this.Left = 200;
            this.Top = 50;
            this.Inputs = [];
            this.Outputs = [];
        }
        else
        {
            // push
            this.Id = id;
            this.Inputs = inputs;
            this.Outputs = outputs;
            this.Name = name;

            // it's about 22 px per input/output
            // need to calc width and height based on the inputs, outputs, and name of the node
            //this.Position = new Point2D(x, y);
            this.Left = x;
            this.Top = y;
            this.resize();
            
            let i;
            for (i =0; i < this.Inputs.length; i++) {
                let n = this.Inputs[i];
                this.Connectors[n] = new GraphNodeConnector(n, true, 0, GraphNode.headerHeight() + i * GraphNode.itemHeight(), this);
            }
            for (i =0; i < this.Outputs.length; i++) {
                let n = this.Outputs[i];
                this.Connectors[n] = new GraphNodeConnector(n, false, this.Width, GraphNode.headerHeight() + (i + this.Inputs.length) * GraphNode.itemHeight(), this);
            }
        }
    }

    static fromJson(json){

        let jsonConnector = json["Connectors"];
        if (jsonConnector!==undefined)
            json.Connectors = {}

        let node = Object.assign(new GraphNode(), json);
        node.resize();

        if (jsonConnector!==undefined)
            Object.keys(jsonConnector).map(key => jsonConnector[key]).forEach(item =>
                {
                    let connector = GraphNodeConnector.fromJson(item, node);
                    node.addConnector(connector);
                });

        return node;
    }

    static areEqual(n1, n2) {
        return n1.Id == n2.Id;
    }
    
    static headerHeight() { return 37; }
    static itemHeight() { return 18; }
    
    // resize height and width dimentions
    resize(){
        this.Width = 10 + this.Name.length * 9; // todo: if a context is supplied, could determine width based on text font?
        this.Height = GraphNode.headerHeight() + (this.Inputs.length + this.Outputs.length) * GraphNode.itemHeight() + 1; // 22 x 3 = 66 112 - 66 = 46
    }
    addConnector(connector) {

        if (connector.Input)
            this.Inputs.push(connector.Name);
        else
            this.Outputs.push(connector.Name);
        
        // TODO: Consider - re-ordering so that inputs are before outputs ??
        this.Connectors[connector.Name] = connector;

        let i;
        for (i =0; i < this.Inputs.length; i++) {
            let n = this.Inputs[i];
            connector.Position.Y = GraphNode.headerHeight() + i * GraphNode.itemHeight();
        }
        for (i =0; i < this.Outputs.length; i++) {
            let n = this.Outputs[i];
            connector.Position.Y = GraphNode.headerHeight() + (i + this.Inputs.length) * GraphNode.itemHeight();
        }
        
        // !Important(!) set the position
        connector.Position.X = connector.Input ? 0 : this.Width;
        
         // 22 x 3 = 66 112 - 66 = 46
        this.Height = GraphNode.headerHeight() + (this.Inputs.length + this.Outputs.length) * GraphNode.itemHeight() + 1;
    }
    
    // create a connector map - position to point, and 
    
    getConnectorPosition(connectorName) {
        
        if (!this.Connectors[connectorName]) {
            var txt = `Node ${this.Name} ${this.Id}: No connector with name = "${connectorName}"`;
            throw txt;
        }
        
        return new Point2D(this.Connectors[connectorName].Position.X + this.Left, this.Connectors[connectorName].Position.Y + this.Top);
    }
    
    intersectsWithRect(rc) {
        if (!rc)
            return false;
        return (rc.Left <= this.Left + this.Width && this.Left <= rc.Right && rc.Top <= this.Top + this.Height && this.Top <= rc.Bottom);
    }
    
    isPointInObject(ctx, x, y) {

        // first check connectors ... 
        for (var c in this.Connectors) {
            let connector = this.Connectors[c];
            let f = connector.isPointInObject(ctx, x, y);
            if (f)
                return f;
        };
        
        let n = this;
        ctx.beginPath();
        ctx.rect(n.Left, n.Top, n.Width, n.Height);
        ctx.closePath();
        // check if we hover it, fill red, if not fill it blue
        if (ctx.isPointInPath(x, y))
            return this;
    }
    
    draw(ctx) {
        //super.draw();
        this.drawNode(ctx, this);
    }

    drawNode(ctx, node) {
        
        const titleTop = 15;
        const separatorLineTop = 23;
        
        ctx.save();

        // ## Shape
        
        // TODO: Decorator - load/save and configurator
        
        // shape fill style
        // #4a5d9e lighter: #687cc4  and again #7884de
        
        if (node.Hover) {
            GraphDecorator.setFillStyle(ctx, "node:hover", node);
        } else if (node.Selected) {
            GraphDecorator.setFillStyle(ctx, "node:selected", node);
        } else {
            GraphDecorator.setFillStyle(ctx, "node", node);
        }

        // TODO: more stuff into decorator like this - then eventually can refactor further
        // set styling for the node
        GraphDecorator.setStyle(ctx, "nodeShadow");
        if (node.Selected)
            GraphDecorator.setStyle(ctx, "nodeLineWidth:selected");
        else
            GraphDecorator.setStyle(ctx, "nodeLineWidth");

        ctx.strokeStyle = node.Selected ? GraphDecorator.getStyle("selected") : "#000"; // "#38436b";

        // Draw the shape
        // todo: decorator: border radius
        // todo: decorator: border color width
        if (!node.Selected)
            ctx.strokeStyle = "rgba(0,0,0,0.2)";
        
        // TODO: also need roundRect to just set path - and not do the fill/stroke - so we can change the shadow
        roundRect(ctx, node.Left, node.Top, node.Width, node.Height, 2, true);

        // ## Title
        
        /*
    font-family: "Consolas", Arial, sans-serif;
    font-size: 12px;
    font-weight: bold;
    color: darkred;
    text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.8);
        */

        let decorator = GraphDecorator.getDecorator();

        // text shadow for title
        if (decorator.textShadow) {
            ctx.shadowColor = decorator.textShadow.shadowColor;
            ctx.shadowOffsetX = decorator.textShadow.shadowOffsetX;
            ctx.shadowOffsetY = decorator.textShadow.shadowOffsetY;
            ctx.shadowBlur = decorator.textShadow.shadowBlur;
        }
        else {
            ctx.shadowOffsetX = 0;
            ctx.shadowOffsetY = 0;
            ctx.shadowColor = "#000";
            ctx.shadowBlur = 0;
        }
        
        ctx.fillStyle = decorator.fontColor;
        ctx.font = `${decorator.fontWeight} ${decorator.fontSize} ${decorator.fontFamily}`;

        var txt = `${node.Name} ${node.Id}`;
        var w = ctx.measureText(txt).width;
        ctx.fillText(txt, node.Left + (node.Width - w) / 2, node.Top + titleTop);

        ctx.font = `${decorator.smallfontSize}px ${decorator.fontFamily}`;
        //console.log(ctx.font);
        
        // ## Inputs and Outputs

        // because c is a "dictionary" of connector names to connectors:
        for (var c in node.Connectors) {
            let connector = node.Connectors[c];
            connector.draw(ctx, node);
        };
        
        // this is the line between title and connectors
        ctx.shadowColor = "#000";
        ctx.shadowOffsetX = 0;
        ctx.shadowOffsetY = -1;
        ctx.shadowBlur = 0;

        // todo: decorator: separator line
        ctx.lineWidth = 1.5;
        ctx.strokeStyle = "rgba(255, 255, 255, 0.2)";
        
        ctx.beginPath();
        // 25 + half of text size??
        ctx.moveTo(node.Left, node.Top + separatorLineTop);
        //ctx.setLineDash([2, 2]);
        ctx.lineTo(node.Left + node.Width, node.Top + separatorLineTop);
        ctx.stroke();

        ctx.restore();
    }
    
}
