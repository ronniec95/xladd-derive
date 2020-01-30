from http.server import HTTPServer, BaseHTTPRequestHandler
import config

class DSRequestHandler(BaseHTTPRequestHandler):

    def do_GET(self):
        if (self.path == "/") or (self.path == "/index.html"):
            self.MainPage()
        elif self.path == "/graph.html" or (self.path == "graph"):
            self.GraphPage()
        elif self.path.startswith("http"):
            if self.path.endswith("js"):
                self.RenderPage('application/javascript', self.path)
            elif self.path.endswith("css"):
                self.RenderPage('text/css', self.path)
            elif self.path.endswith("png"):
                self.RenderPage('text/png', self.path)
        elif self.path.startswith("/js/"):
            self.RenderPage('application/javascript', "./"+self.path)
        elif self.path.startswith("/image/"):
            self.RenderPage('image/png',  "./"+self.path)
        elif self.path.startswith("/css/"):
            self.RenderPage('text/css',  "./"+self.path)
        elif self.path == "/api/routes":
            self.RoutesPage()
        elif self.path == "/api/outputqs":
            self.OutputQsPage()
        elif self.path == "/api/inputqs":
            self.InputQsPage()
        elif self.path == "/api/outputchannels":
            self.OutputChannelJSON()
        elif self.path == "/api/inputchannels":
            self.InputQsPage()
        elif (self.path == "/time") or (self.path == "/clock"):
            self.ClockPage()
        else:
            self.BadPage()

    def RenderPage(self, contenttype, path):
        contentfile = open(path, 'rb')
        self.send_response(200)
        self.send_header('Content-type', contenttype)
        self.end_headers()
        self.wfile.write(contentfile.read())

    def RoutesPage(self):
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        json_str = config.MSM.MeshNodesRoutesData()
        self.wfile.write(json_str.encode(encoding='utf_8'))

    def OutputChannelJSON(self):
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        json_str = config.MSM.CreateGraphNodes()
        self.wfile.write(json_str.encode(encoding='utf_8'))

    def InputQsPage(self):
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        json_str = config.MSM.OutputQPayload()
        self.wfile.write(json_str.encode(encoding='utf_8'))

    def OutputQsPage(self):
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        json_str = config.MSM.InputQPayload()
        self.wfile.write(json_str.encode(encoding='utf_8'))

    def ClockPage(self):
        self.send_response(200)
        self.send_header('Content-Type', 'text/html')
        self.end_headers()
        badhtml = open("./html/clock.html", 'rb')
        self.wfile.write(badhtml.read())

    def BadPage(self):
        self.send_response(200)
        self.send_header('Content-Type', 'text/html')
        self.end_headers()
        badhtml = open("./html/bad.html", 'rb')
        self.wfile.write(badhtml.read())

    def GraphPage(self):
        aarcheader = open("./graph.html", 'rb')
        self.send_response(200)
        self.send_header('Content-type',    'text/html')
        self.end_headers()
        self.wfile.write(aarcheader.read())

    def MainPage(self):
        aarcheader = open("./html/aarcheader.html", 'rb')
        aarcbody = open("./html/aarcbody2.html", 'rb')
        aarcgraph = open("./js/aarcgraph.js", 'rb')
        visutils = open("./js/visutils.js", 'rb')
        javascript = """\n<script type="text/javascript">\n"""

        self.send_response(200)
        self.send_header('Content-type',    'text/html')
        self.end_headers()
        self.wfile.write("<html>\n".encode())
        self.wfile.write(aarcheader.read())
        self.wfile.write("<body>\n".encode())
        self.wfile.write(aarcbody.read())
        self.wfile.write(javascript.encode())
        self.wfile.write(config.MSM.MeshNodesRoutes().encode())
        self.wfile.write(aarcgraph.read())
        self.wfile.write(visutils.read())
        self.wfile.write("""\n</script>""".encode())
        self.wfile.write("\n</body>".encode())
        self.wfile.write("\n</html>".encode())
        print (config.MSM.MeshRoutes())
        print (config.MSM.MeshNodesRoutes())
