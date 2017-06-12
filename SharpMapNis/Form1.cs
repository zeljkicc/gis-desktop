using BruTile;
using BruTile.Predefined;
using BruTile.Web;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using GISprojekat4.Configuration;
using GISprojekat4.StyleForms;
using MetroFramework.Forms;
using NetTopologySuite.IO;
//using NetTopologySuite.Geometries;
using Npgsql;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Symbolizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace GISprojekat4
{
    public partial class Form1 : MetroForm
    {
        private Configuration.Configuration configuration = new Configuration.Configuration();


        private string _selectedMenuItem;
        private readonly ContextMenuStrip collectionRoundMenuStrip;

        private string connstring = String.Format("Server={0};Port={1};" +
            "User Id={2};Password={3};Database={4};",
            "127.0.0.1", "5432", "postgres",
            "admin", "serbia");
        private string geomname = "geom";
        private string idname = "gid";

        bool mouseDown = false;

        VectorLayer poisLayer;
        VectorLayer tourDataLayer;

        LabelLayer poisLabelLayer;
        LabelLayer buildingsLabelLayer;

        Dictionary<string, string> layerTypes = new Dictionary<string, string>();
        Dictionary<string, string> layerTableNames = new Dictionary<string, string>();

        public object MapTools { get; private set; }

        private string _layerDataInTree = null;

        private string _queriedLayerName = null;

        public Form1()
        {
            
            InitializeComponent();

            this.Text = "SharpMapNiš Tourist Guide";

            //sugestije searchbox-a
            searchComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
            searchComboBox.DroppedDown = true;
            searchComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;            

            //kontekstni meni////////
            var toolStripMenuItem1 = new ToolStripMenuItem { Text = "Labels" };
            toolStripMenuItem1.Click += toolStripMenuItem1_Click;
            var toolStripMenuItem2 = new ToolStripMenuItem { Text = "Style" };
            toolStripMenuItem2.Click += toolStripMenuItem2_Click;
            collectionRoundMenuStrip = new ContextMenuStrip();
            collectionRoundMenuStrip.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, toolStripMenuItem2 });
            checkedListBox1.MouseDown += checkedListBox1_MouseDown;


            layerListComboBox.Enabled = false;
            layerAttributeListComboBox.Enabled = false;
            comboBoxValue.Enabled = false;
            layerQueryButton.Enabled = false;

            //filtriranje po tipu
            typeComboBox.Items.Add("All");
            typeComboBox.Items.Add("restaurant");
            typeComboBox.Items.Add("hotel");
            typeComboBox.Items.Add("coffeepub");
            typeComboBox.Items.Add("monument");
            typeComboBox.Items.Add("culture");
            typeComboBox.Items.Add("cinema");

            this.checkedListBox1.AllowDrop = true;

            //povezivanje kontrola za zoom i query sa mapBox kontrolom
            mapZoomToolStrip1.MapControl = mapBox1;
            mapQueryToolStrip1.MapControl = mapBox1;

            ///////sloj turistickih objekata////////////////////////////////////////////////
            tourDataLayer = new VectorLayer("Tourist locations");
            //definisanje stila sloja turistickih podataka
            SharpMap.Styles.VectorStyle restaurantStyle = new SharpMap.Styles.VectorStyle();
            restaurantStyle.Symbol = Image.FromFile("images/restaurant.png");

            SharpMap.Styles.VectorStyle hotelStyle = new SharpMap.Styles.VectorStyle();
            hotelStyle.Symbol = Image.FromFile("images/hotel1.png");

            SharpMap.Styles.VectorStyle cinemaStyle = new SharpMap.Styles.VectorStyle();
            cinemaStyle.Symbol = Image.FromFile("images/cinema.png");

            SharpMap.Styles.VectorStyle monumentStyle = new SharpMap.Styles.VectorStyle();
            monumentStyle.Symbol = Image.FromFile("images/monument.png");

            SharpMap.Styles.VectorStyle coffeeStyle = new SharpMap.Styles.VectorStyle();
            coffeeStyle.Symbol = Image.FromFile("images/coffee.png");

            SharpMap.Styles.VectorStyle cultureStyle = new SharpMap.Styles.VectorStyle();
            cultureStyle.Symbol = Image.FromFile("images/culture.png");

            SharpMap.Styles.VectorStyle defaultTourDataStyle = new SharpMap.Styles.VectorStyle();
            defaultTourDataStyle.PointColor = new SolidBrush(Color.Purple);

            Dictionary<string, SharpMap.Styles.IStyle> styles = new Dictionary<string, SharpMap.Styles.IStyle>();
            styles.Add("restaurant", restaurantStyle);
            styles.Add("hotel", hotelStyle);
            styles.Add("cinema", cinemaStyle);
            styles.Add("monument", monumentStyle);
            styles.Add("coffeepub", coffeeStyle);
            styles.Add("culture", cultureStyle);

            tourDataLayer.Theme = new SharpMap.Rendering.Thematics.UniqueValuesTheme<string>("type", styles, defaultTourDataStyle);
            loadLayerBackground(tourDataLayer, "tour_data");
            layerTableNames.Add("Tourist locations", "tour_data");
            layerTypes.Add("Tourist locations", "point");
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            ///postavljanje base layer mape, koriscenjem osm tile servera
            var tileSource = new HttpTileSource(new BruTile.Predefined.GlobalSphericalMercator(YAxis.OSM, 0, 18),
                "http://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
                new[] { "a", "b", "c" }, "OSM");
            TileAsyncLayer tal = new TileAsyncLayer(tileSource, "OSM");
            mapBox1.Map.BackgroundLayer.Add(tal);

            /////////////////postavljanje zoom-a, centra i maximalnog extenta prikaza
            /////////////////(ogranicavanje na nis)
            mapBox1.Map.Zoom = 3500;

            var ctFac1 = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory();
            var csSrc1 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;//4326
            var csTgt1 = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator;//3857
            var ct = ctFac1.CreateFromCoordinateSystems(csSrc1, csTgt1);
            var ptWebMercator1 = ct.MathTransform.Transform(new double[] { 21.855482, 43.347950 });
            var ptWebMercator2 = ct.MathTransform.Transform(new double[] { 21.929629, 43.300383 });

            mapBox1.Map.MaximumExtents = new Envelope(
                new Coordinate(ptWebMercator1[0], ptWebMercator1[1]),
                new Coordinate(ptWebMercator2[0], ptWebMercator2[1]));
            mapBox1.Map.EnforceMaximumExtents = true;

            var ptWebMercatorCenter = ct.MathTransform.Transform(new double[] { 21.895898, 43.321451 });

            mapBox1.Map.Center = new Coordinate(ptWebMercatorCenter[0], ptWebMercatorCenter[1]);
            mapBox1.Refresh();
            ///////////////////////////////////////////////////////

        }

        private void mapBox1_MouseMove(GeoAPI.Geometries.Coordinate worldPos, MouseEventArgs imagePos)
        {
            //prikaza koordinata na poziciji miza u GK i geografskim koordinatama
            var ctFac = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory();
            ////////////////Web Mercator
            GeoAPI.Geometries.Coordinate point = mapBox1.Map.ImageToWorld(new PointF(imagePos.X, imagePos.Y));
            /////
            var csSrc1 = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator;

            ///////////////////Gauss-Krugerova (Transverse Mercator)
            var csTgt1 = CreateUtmProjection(34);
            var ct1 = ctFac.CreateFromCoordinateSystems(csSrc1, csTgt1);
            var ptGK = ct1.MathTransform.Transform(new double[] { point.X, point.Y });

            label1.Text = ptGK[0] + " : " + ptGK[1];

            //////////////////WGS84 (Geographic)       
            var csTgt2 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;
            var ct2 = ctFac.CreateFromCoordinateSystems(csSrc1, csTgt2);
            var ptWGS84 = ct2.MathTransform.Transform(new double[] { point.X, point.Y });

            label2.Text = ptWGS84[0] + " : " + ptWGS84[1]; 
        }


        public void loadLayerBackground(VectorLayer layer, string layerName)
        {
            //ucitavanje slojeva u Background Worker-ima    
            queryLayer1ComboBox.Items.Add(layer.LayerName);
            queryLayer2ComboBox.Items.Add(layer.LayerName);

            var bw = new BackgroundWorker();
            bw.DoWork += delegate
            {

                layer.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                PostGIS postgis = new PostGIS(connstring, layerName, geomname, idname);

                //postavljanje envelopa kao Definition Query, da se ogranici broj featurea
                var ctFac1 = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory();
                var csSrc1 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;//4326
                var csTgt1 = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator;//3857
                var ct = ctFac1.CreateFromCoordinateSystems(csSrc1, csTgt1);
                var ptWebMercator1 = ct.MathTransform.Transform(new double[] { 21.855482, 43.347950 });
                var ptWebMercator2 = ct.MathTransform.Transform(new double[] { 21.929629, 43.300383 });

                mapBox1.Map.MaximumExtents = new Envelope(
                    new Coordinate(ptWebMercator1[0], ptWebMercator1[1]),
                    new Coordinate(ptWebMercator2[0], ptWebMercator2[1]));
                postgis.DefinitionQuery = "geom && ST_MakeEnvelope(" + ptWebMercator1[0] + ", " + ptWebMercator1[1] + ", " + ptWebMercator2[0] + ", " + ptWebMercator2[1] + ", " + "3857)";

                layer.DataSource = postgis;

                int x = 0;
            };
            //  bw.ProgressChanged += delegate { ... };
            bw.RunWorkerCompleted += delegate
            {
                //kada se zavrsi sa ucitavanjem, dodamo ga na mapu
                mapBox1.Map.Layers.Add(layer);
                checkedListBox1.Items.Add(layer.LayerName);

                if (layer.LayerName != "Tourist locations")
                {
                    layer.Enabled = false;
                }
                else
                {
                    checkedListBox1.SetItemCheckState(checkedListBox1.Items.
                        IndexOf("Tourist locations"), CheckState.Checked);
                }

            };
            bw.RunWorkerAsync();
        }

        public void loadLabelLayerBackground(LabelLayer layer, string layerName)
        {
            //ucitavanje label slojeva u pozadini
            var bw = new BackgroundWorker();
            bw.DoWork += delegate
            {
                PostGIS postgis = new PostGIS(connstring, layerName, geomname, idname);
                layer.DataSource = postgis;
            };
            bw.RunWorkerCompleted += delegate
            {
                mapBox1.Map.Layers.Add(layer);
                checkedListBox1.Items.Add(layer.LayerName);
                layer.Enabled = false;
            };
            bw.RunWorkerAsync();
        }

        private void checkedListBox1_EnabledChanged(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string layerName = (string)checkedListBox1.Items[e.Index];

            if (e.NewValue == CheckState.Checked)//cekirano
            {
                mapBox1.Map.Layers.Where(l => l.LayerName == layerName).First().Enabled = true;
            }
            else//otcekirano
            {
                mapBox1.Map.Layers.Where(l => l.LayerName == layerName).First().Enabled = false;
            }

            mapBox1.Refresh();

        }

        private void checkedListBox1_DragDrop(object sender, DragEventArgs e)
        {
            //tacka gde dropujemo item
            Point point = checkedListBox1.PointToClient(new Point(e.X, e.Y));
            //redni broj u nizu itema
            int indexEnd = this.checkedListBox1.IndexFromPoint(point);
            if (indexEnd < 0) indexEnd = this.checkedListBox1.Items.Count - 1;

            int indexStart = checkedListBox1.Items.IndexOf(checkedListBox1.SelectedItem);

            if (indexStart != indexEnd)
            {
                mouseDown = false;

                bool stateStart = checkedListBox1.GetItemChecked(indexStart);
                //bool stateEnd = checkedListBox1.GetItemChecked(indexEnd);

                object data = checkedListBox1.SelectedItem; //odakle prenosimo (koja je selektovana)
                this.checkedListBox1.Items.Remove(data);
                this.checkedListBox1.Items.Insert(indexEnd, data);//index - na koje mesto

                //cuvanje stanja da li je stavka cekirana ili ne
                checkedListBox1.SetItemChecked(indexEnd, stateStart);

                VectorLayer temp1 = (VectorLayer)mapBox1.Map.Layers[indexStart];
                //   VectorLayer temp2 = (VectorLayer)mapBox1.Map.Layers[indexEnd];
                mapBox1.Map.Layers.RemoveAt(indexStart);
                mapBox1.Map.Layers.Insert(indexEnd, temp1);

                mapBox1.Refresh();
            }
            else
            {
                CheckState cs = checkedListBox1.GetItemCheckState(indexStart);
                if (cs == CheckState.Checked)
                    checkedListBox1.SetItemChecked(indexStart, false);
                else checkedListBox1.SetItemChecked(indexStart, true);

            }
        }


        private void checkedListBox1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
            //DoDragDrop(this.listBox1.SelectedItem, DragDropEffects.Link);
        }

        private void checkedListBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //context menu
            if (e.Button == MouseButtons.Right)
            {
                var index = checkedListBox1.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    _selectedMenuItem = checkedListBox1.Items[index].ToString();
                    collectionRoundMenuStrip.Show(Cursor.Position);
                    collectionRoundMenuStrip.Visible = true;
                }
                else
                {
                    collectionRoundMenuStrip.Visible = false;
                }
            }
            else
            {
                mouseDown = true;
                if (this.checkedListBox1.SelectedItem == null) return;
                // this.checkedListBox1.SetItemChecked(this.checkedListBox1.Items.IndexOf(this.checkedListBox1.SelectedItem), true);
                this.checkedListBox1.DoDragDrop(this.checkedListBox1.SelectedItem, DragDropEffects.Move);
            }
        }

        private void checkedListBox1_MouseUp(object sender, MouseEventArgs e)
        {
            /*   if (mouseDown)
               {
                   this.checkedListBox1.SetItemChecked(this.checkedListBox1.Items.IndexOf(this.checkedListBox1.SelectedItem),
                       !this.checkedListBox1.GetItemChecked(this.checkedListBox1.Items.IndexOf(this.checkedListBox1.SelectedItem)));
                   mouseDown = false;
               } */
        }


        ////////////////////////////////////////////////////////////
        //context menu
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
  
            string type;
            this.layerTypes.TryGetValue(_selectedMenuItem, out type);
            string layerName;
            this.layerTableNames.TryGetValue(_selectedMenuItem, out layerName);
            switch (type)
            {
                case "line":

                    using (var lineStyleForm = new LineStyleForm())
                    {
                        var result = lineStyleForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            VectorLayer vl = (VectorLayer)mapBox1.Map.Layers.GetLayerByName(_selectedMenuItem);
                            vl.Style.Line.Color = lineStyleForm.lineColor;
                            vl.Style.Line.DashStyle = lineStyleForm.lineStyle;
                            vl.Style.Line.Width = lineStyleForm.lineWidth;
                            mapBox1.Refresh();

                            //cuvanje konfiguracije
                            LineLayerConfiguration llc = new LineLayerConfiguration()
                            {
                                color = lineStyleForm.lineColor,
                                type = lineStyleForm.lineStyle,
                                width = lineStyleForm.lineWidth,
                                name = layerName
                            };

                            configuration.lineLayersDictionary[layerName] = llc;
                            configuration.saveConfiguration("line", layerName);
                        }
                    }

                    break;

                case "point":
                    using (var dotStyleForm = new DotStyleForm())
                    {
                        var result = dotStyleForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            //trazimo odgovarajuci vektorski sloj, kome menjamo stil, sto smo kliknuli desni klik na njemu
                            VectorLayer vl = (VectorLayer)mapBox1.Map.Layers.GetLayerByName(_selectedMenuItem);

                            if (dotStyleForm.setSymbol)
                            {
                                if (dotStyleForm.pointSymbol != null)
                                    vl.Style.Symbol = dotStyleForm.pointSymbol;
                            }
                            else
                            {
                                vl.Style.Symbol = null;
                                vl.Style.PointColor = new SolidBrush(dotStyleForm.pointColor);
                            }
                            mapBox1.Refresh();


                            //cuvanje konfiguracije
                            PointLayerConfiguration plc = new PointLayerConfiguration()
                            {
                                color = dotStyleForm.pointColor,
                                symbolUri = dotStyleForm.symbolUri,
                                name = layerName
                            };

                            configuration.pointLayersDictionary[layerName] = plc;
                            configuration.saveConfiguration("point", layerName);
                        }
                    }
                    break;

                case "polygon":
                    using (var polygonStyleForm = new PolygonStyleForm())
                    {
                        var result = polygonStyleForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            //trazimo odgovarajuci vektorski sloj, kome menjamo stil, sto smo kliknuli desni klik na njemu
                            VectorLayer vl = (VectorLayer)mapBox1.Map.Layers.GetLayerByName(_selectedMenuItem);

                            vl.Style.Fill = new SolidBrush(polygonStyleForm.fillColor);
                            vl.Style.EnableOutline = true;
                            vl.Style.Outline= new Pen(polygonStyleForm.strokeColor);

                            if (polygonStyleForm.setPattern)
                            {
                               

                                vl.Style.Fill = new HatchBrush(polygonStyleForm.hatchStyle, Color.Black, polygonStyleForm.fillColor);
                            }
                            
                          

                            mapBox1.Refresh();


                            //cuvanje konfiguracije
                          /*  PointLayerConfiguration plc = new PointLayerConfiguration()
                            {
                                color = dotStyleForm.pointColor,
                                symbolUri = dotStyleForm.symbolUri,
                                name = layerName
                            };

                            configuration.pointLayersDictionary[layerName] = plc;
                            configuration.saveConfiguration("point", layerName); */
                        }
                    }
                    break;
            }

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string tableName;
            this.layerTableNames.TryGetValue(_selectedMenuItem, out tableName);


            using (var labelsForm = new LabelsForm(tableName))
            {
                var result = labelsForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    // VectorLayer vl = (VectorLayer)mapBox1.Map.Layers.GetLayerByName(_selectedMenuItem);
                    // vl.Style.Line.Color = lineStyleForm.lineColor;
                    //  vl.Style.Line.DashStyle = lineStyleForm.lineStyle;
                    //  vl.Style.Line.Width = lineStyleForm.lineWidth;
                    //  mapBox1.Refresh();
                    //this.poisLabelLayer.LabelColumn = labelsForm.columnName;
                    LabelLayer ll = null;
                    if (mapBox1.Map.Layers.GetLayerByName(_selectedMenuItem + " Labels") != null)
                    {
                        ll = ((LabelLayer)mapBox1.Map.Layers.GetLayerByName(_selectedMenuItem + " Labels"));
                    }
                    else
                    {
                        ll = new LabelLayer(_selectedMenuItem + " Labels");
                        ll.Style.CollisionDetection = true;
                        ll.Style.CollisionBuffer = new SizeF(10, 10);
                        ll.LabelFilter = SharpMap.Rendering.LabelCollisionDetection.ThoroughCollisionDetection;

                        //ll.Style.VerticalAlignment = SharpMap.Styles.LabelStyle.VerticalAlignmentEnum.Bottom;
                       
                        loadLabelLayerBackground(ll, tableName);

                        
                    }
                    ll.Style.Offset = new PointF(labelsForm.horizontalOffset, labelsForm.verticalOffset);

                    ll.Style.ForeColor = labelsForm.color;

                    ll.Style.Font = new Font(new FontFamily(labelsForm.fontFamily), labelsForm.fontSize);

                    ll.LabelColumn = labelsForm.columnName;

                    mapBox1.Refresh();
                    //na drugo mesto

                }
            }



        }

        private IProjectedCoordinateSystem CreateUtmProjection(int utmZone)
        {
            CoordinateSystemFactory cFac = new CoordinateSystemFactory();
            IEllipsoid ellipsoid = cFac.CreateFlattenedSphere("WGS 84", 6378137, 298.257223563, LinearUnit.Metre);
            IHorizontalDatum datum = cFac.CreateHorizontalDatum("WGS_1984", DatumType.HD_Geocentric, ellipsoid, null);
            IGeographicCoordinateSystem gcs = cFac.CreateGeographicCoordinateSystem("WGS 84", AngularUnit.Degrees, datum, PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East), new AxisInfo("Lat", AxisOrientationEnum.North));

            List<ProjectionParameter> parameters = new List<ProjectionParameter>();
            parameters.Add(new ProjectionParameter("latitude_of_origin", 0));
            parameters.Add(new ProjectionParameter("central_meridian", -183 + 6 * utmZone));
            parameters.Add(new ProjectionParameter("scale_factor", 0.9996));
            parameters.Add(new ProjectionParameter("false_easting", 7500000));
            parameters.Add(new ProjectionParameter("false_northing", 0.0));
            IProjection projection = cFac.CreateProjection("Transverse Mercator", "Transverse_Mercator", parameters);
            return cFac.CreateProjectedCoordinateSystem("WGS 84 / UTM zone " + utmZone.ToString() + "N", gcs,
               projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East),
               new AxisInfo("North", AxisOrientationEnum.North));
        }

        private void mapBox1_MouseUp(GeoAPI.Geometries.Coordinate worldPos, MouseEventArgs imagePos)
        {
            var p = mapBox1.Map.ImageToWorld(imagePos.Location);
            FeatureDataSet ds = new FeatureDataSet();

            foreach (var layer in mapBox1.Map.Layers)
            {
                //Test if the layer is queryable?
                var queryLayer = layer as SharpMap.Layers.ICanQueryLayer;
                // if (queryLayer != null)
                //  queryLayer.ExecuteIntersectionQuery(p., ds);
            }
        }




  
        FeatureDataRow fdr1;
        FeatureDataRow fdr2;
        int countFDR = 0;



        private FeatureDataTable _data;
        private FeatureDataTable _selectedFeaturesTable1;
        private FeatureDataTable _selectedFeaturesTable2;
        private void mapBox1_MapQueried(FeatureDataTable data)
        {
            if (mapBox1.Map.GetLayerByName("QueriedFeatures") != null)
            {
                checkedListBox1.Items.Remove("QueriedFeatures");
                mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("QueriedFeatures"));
            }




            if (data.TableName == "Tourist locations" || data.TableName == "tour_data")
            {
                metroLabel6.Text = "";
                if(data.Count == 0)
                {
                    MessageBox.Show("No objects fount. Try again.");
                    return;
                }
                labelTDName.Text = data[0].ItemArray[3].ToString();
                labelTDType.Text = data[0].ItemArray[2].ToString();
                labelTDDescription.Text = data[0].ItemArray[4].ToString();
                labelTDOpenFrom.Text = data[0].ItemArray[5].ToString();
                labelTDOpenTo.Text = data[0].ItemArray[6].ToString();

                pictureBoxTD.LoadAsync(data[0].ItemArray[7].ToString());

                //linkTDWebsite.
            }

            _data = data;
 

            if (checkBoxRoutingEnable.Checked)
            {
                string layerType = layerTypes[data.TableName];
                if (layerType == "point")
                    if (fdr1 == null)
                    {
                        fdr1 = data[0];
                        if (data.TableName == "Tourist locations")
                        {
                            routeStartTextBox.Text = (string)data[0].ItemArray[3];
                        }
                        else
                        {
                            if (data[0].ItemArray[4].ToString() == "")
                            {
                                routeStartTextBox.Text = "Starting point";
                            }
                            else
                            {
                                routeStartTextBox.Text = data[0].ItemArray[4].ToString();
                            }

                        }
                    }
                    else
                    {
                        fdr2 = data[0];
                        if (data.TableName == "Tourist locations")
                        {
                            routeEndTextBox.Text = (string)data[0].ItemArray[3];
                        }
                        else
                        {
                            if (data[0].ItemArray[4].ToString() == "")
                            {
                                routeEndTextBox.Text = "End point";
                            }
                            else
                            {
                                routeEndTextBox.Text = data[0].ItemArray[4].ToString();
                            }

                        }
                    
                    }
            
            }


            
                string[] columns = new string[data.Columns.Count];
                for (int j = 0; j < data.Columns.Count; j++)
                {
                    columns[j] = data.Columns[j].ColumnName;
                }


            

                treeView1.Nodes.Clear();
                this._layerDataInTree = data.TableName;
                int count = 0;
                foreach (FeatureDataRow fdr in data)
                {
                    object[] values = fdr.ItemArray;
                    TreeNode[] array = new TreeNode[values.Count()];
                    for (int i = 0; i < values.Count(); i++)
                    {
                        array[i] = new TreeNode(columns[i] + ": " + values[i].ToString());
                    }
                TreeNode treeNode = null;
                if(fdr.Table.TableName == "Tourist locations" || fdr.Table.TableName == "tour_data")
                {
                    treeNode = new TreeNode(values[3].ToString(), array);
                }
                else 
                {
                    treeNode = new TreeNode(values[4].ToString(), array);
                }
                    if(treeNode != null)
                    treeView1.Nodes.Add(treeNode);
                }


            /* List<string> list = new List<string>();
             for (int i = 0; i < data.Columns.Count; i++)
             {
                 list.Add(columns[i] + ": " + data[0].ItemArray[i].ToString());

                 metroListView1.Items.Add(new ListViewItem(columns[i])).SubItems.Add(data[0].ItemArray[i].ToString());
             } */


            //listBox1.DataSource = list;

            GeometryFeatureProvider _geometryProvider = new SharpMap.Data.Providers.GeometryFeatureProvider(data);
            VectorLayer layer = new SharpMap.Layers.VectorLayer("QueriedFeatures", _geometryProvider);
            // foreach()
            if (enableSpatialQueriesCheckBox.Checked &&
                (queryLayer1ComboBox.SelectedItem == null ||
                queryLayer2ComboBox.SelectedItem == null ||
                queryLayer1ComboBox.SelectedItem.ToString() == "Selected Features" ||
                queryLayer2ComboBox.SelectedItem.ToString() == "Selected Features"))
            {

            }
            else
            {
             
                /* VectorLayer layer = new SharpMap.Layers.VectorLayer("QueriedFeatures", _geometryProvider);
             // _layer.IsQueryEnabled = false;

             mapBox1.Map.Layers.Add(layer);
             mapBox1.Refresh(); */

             
                if (mapBox1.Map.GetLayerByName("QueriedFeatures") != null)
                {
                    checkedListBox1.Items.Remove("QueriedFeatures");
                    mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("QueriedFeatures"));
                }

                
                // _layer.IsQueryEnabled = false;

                //postavljamo stilove u slucaju kada su selektovani objekti
                layer.Style.PointColor = new SolidBrush(Color.DarkRed);
                layer.Style.Fill = new HatchBrush(HatchStyle.Horizontal, Color.DarkRed);
                layer.Style.Line.Color = Color.DarkRed;
                layer.Style.Line.Width = 3.0f;


                //da li predstavlja problem????????????????????????
                if (data.TableName == "Tourist locations"
                    || data.TableName == "tour_data")
                {
                    layer.Theme = _tourDataSelectedTheme;
                }

                layer.Enabled = false;
                mapBox1.Map.Layers.Add(layer);
                mapBox1.Refresh();
                checkedListBox1.Items.Add(layer.LayerName);
                checkedListBox1.SetItemCheckState(checkedListBox1.Items.
                        IndexOf(layer.LayerName), CheckState.Checked);
            }

            

            if (enableSpatialQueriesCheckBox.Checked)
            {
                if(queryLayer1ComboBox.SelectedItem == null || queryLayer1ComboBox.SelectedItem.ToString() == "Select Features" )
                {
                    if (mapBox1.Map.GetLayerByName("SelectedFeatures1(Blue)") != null)
                    {
                        mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("SelectedFeatures1(Blue)"));
                    }




                    if (checkedListBox1.Items.Contains("SelectedFeatures1(Blue)"))
                        checkedListBox1.Items.Remove("SelectedFeatures1(Blue)");

     










                    if (!queryLayer1ComboBox.Items.Contains("SelectedFeatures1(Blue)"))
                    queryLayer1ComboBox.Items.Add("SelectedFeatures1(Blue)");

                    
                    VectorLayer layer1 = new SharpMap.Layers.VectorLayer("SelectedFeatures1(Blue)", _geometryProvider);

                    layer1.Style.PointColor = new SolidBrush(Color.Blue);
                    layer1.Style.Line.Color = Color.Blue;
                    layer1.Style.Fill = new SolidBrush(Color.Blue);

                    layer1.Enabled = false;
                    mapBox1.Map.Layers.Add(layer1);
                    mapBox1.Refresh();
                    

                    if (!checkedListBox1.Items.Contains("SelectedFeatures1(Blue)"))
                    checkedListBox1.Items.Add(layer1.LayerName);

                    checkedListBox1.SetItemCheckState(checkedListBox1.Items.
                        IndexOf(layer1.LayerName), CheckState.Checked);


                    queryLayer1ComboBox.SelectedItem = "SelectedFeatures1(Blue)";

                    _selectedFeaturesTable1 = data;
                }
                else
                {
                    if (queryLayer2ComboBox.SelectedItem == null || queryLayer2ComboBox.SelectedItem.ToString() == "Select Features")
                    {
                        if (mapBox1.Map.GetLayerByName("SelectedFeatures2(Green)") != null)
                        {
                            mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("SelectedFeatures2(Green)"));
                        }

                        if (!queryLayer1ComboBox.Items.Contains("SelectedFeatures2(Green)"))
                        queryLayer2ComboBox.Items.Add("SelectedFeatures2(Green)");


                        if (checkedListBox1.Items.Contains("SelectedFeatures2(Green)"))
                            checkedListBox1.Items.Remove("SelectedFeatures2(Green)");


                        VectorLayer layer2 = new SharpMap.Layers.VectorLayer("SelectedFeatures2(Green)", _geometryProvider);

                        layer2.Style.PointColor = new SolidBrush(Color.Green);
                        layer2.Style.Line.Color = Color.Green;
                        layer2.Style.Fill = new SolidBrush(Color.Green);

                        layer2.Enabled = false;
                        mapBox1.Map.Layers.Add(layer2);
                        mapBox1.Refresh();
                       

                        if (!checkedListBox1.Items.Contains("SelectedFeatures1(Green)"))
                            checkedListBox1.Items.Add(layer2.LayerName);

                        checkedListBox1.SetItemCheckState(checkedListBox1.Items.
                       IndexOf(layer2.LayerName), CheckState.Checked);

                        queryLayer2ComboBox.SelectedItem = "SelectedFeatures2(Green)";

                        _selectedFeaturesTable2 = data;
                    }
                }
                

                
            }

            if(!checkBoxRoutingEnable.Checked && !enableSpatialQueriesCheckBox.Checked)
            {
                if(mapBox1.Map.GetLayerByName("QueriedFeatures").Envelope.Grow(20).Height >= mapBox1.Map.MaximumExtents.Height
                    || mapBox1.Map.GetLayerByName("QueriedFeatures").Envelope.Grow(20).Width >= mapBox1.Map.MaximumExtents.Width)
                {
                    mapBox1.Map.ZoomToBox(mapBox1.Map.GetLayerByName("QueriedFeatures").Envelope);
                }
                else
                {
                    mapBox1.Map.ZoomToBox(mapBox1.Map.GetLayerByName("QueriedFeatures").Envelope.Grow(20));
                }
                
                mapBox1.Refresh();
            }
            
        }

        private void layerQueryCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            layerListComboBox.Enabled = true;
            layerAttributeListComboBox.Enabled = true;
            comboBoxValue.Enabled = true;
            layerQueryButton.Enabled = true;

            if (layerQueryCheckBox.Checked)
            {
                foreach (ILayer layer in mapBox1.Map.Layers)
                {
                    layerListComboBox.Items.Add(layer.LayerName);
                }

            }
            else
            {
                layerListComboBox.Enabled = false;
                layerAttributeListComboBox.Enabled = false;
                comboBoxValue.Enabled = false;
                layerQueryButton.Enabled = false;
            }
        }

        private void layerListComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            int x = 0;

            string selectedLayerName = this.layerListComboBox.SelectedItem.ToString();

            string selectedLayerTableName;
            this.layerTableNames.TryGetValue(selectedLayerName, out selectedLayerTableName);

            string connstring = String.Format("Server={0};Port={1};" +
           "User Id={2};Password={3};Database={4};",
           "127.0.0.1", "5432", "postgres",
           "admin", "serbia");

            // Connect to a PostgreSQL database
            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            // Define a query
            NpgsqlCommand command = new NpgsqlCommand("SELECT column_name from information_schema.columns where table_name = '" + selectedLayerTableName + "'", conn);

            // Execute the query and obtain a result set
            NpgsqlDataReader dr = command.ExecuteReader();

            this.layerAttributeListComboBox.Items.Clear();
            this.comboBoxValue.Items.Clear();
            // Output rows
            while (dr.Read())
                this.layerAttributeListComboBox.Items.Add(dr[0]);

            conn.Close();
        }

        private void layerQueryButton_Click(object sender, EventArgs e)
        {
            if(comboBoxValue.SelectedItem == null && comboBoxValue.Text == "")
            {
                MessageBox.Show("Input or select a value");
                return;
            }
            string selectedLayerName = this.layerListComboBox.SelectedItem.ToString();

            string selectedLayerTableName;
            this.layerTableNames.TryGetValue(selectedLayerName, out selectedLayerTableName);

            if (mapBox1.Map.GetLayerByName("QueriedFeatures") != null)
            {
               checkedListBox1.Items.Remove("QueriedFeatures");
                mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("QueriedFeatures"));
            }

             VectorLayer vl = new VectorLayer("QueriedFeatures");
            PostGIS posGisProv = new PostGIS(this.connstring, selectedLayerTableName, "geom", "gid");


            var bbox = posGisProv.GetExtents();
            string queryValue = null;
            if (operatorsComboBox1.SelectedItem.ToString() == "LIKE" || operatorsComboBox1.SelectedItem.ToString() == "NOT LIKE")
            {
                if(comboBoxValue.SelectedItem!=null)
                {
                    queryValue = "%" + comboBoxValue.SelectedItem.ToString().ToLower() + "%";
                   
                }
                else
                {
                    queryValue = "%" + comboBoxValue.Text.ToLower() + "%";
                }
                posGisProv.DefinitionQuery = layerAttributeListComboBox.SelectedItem + " "
                + operatorsComboBox1.SelectedItem.ToString() + " LOWER('" + queryValue + "') AND"
                + " (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))";




            }
            else
            {
                if (comboBoxValue.SelectedItem != null)
                {
                    queryValue = comboBoxValue.SelectedItem.ToString();

                   
                }
                else
                {
                    queryValue = comboBoxValue.Text.ToString();
                }

                posGisProv.DefinitionQuery = layerAttributeListComboBox.SelectedItem + " "
                    + operatorsComboBox1.SelectedItem + " '" + queryValue + "' AND"
                   + " (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))";


            }

           
             
            

                var ds = new SharpMap.Data.FeatureDataSet();
            posGisProv.ExecuteIntersectionQuery(bbox, ds);
          /*  if (ds.Tables[0].Rows.Count > 0)
            {
                SharpMap.Data.FeatureDataRow selectedFeature = ds.Tables[0].Rows[0] as SharpMap.Data.FeatureDataRow;
            } */

            this.mapBox1_MapQueried(ds.Tables[0]);

            








        }

        private void typeComboBox_DisplayMemberChanged(object sender, EventArgs e)
        {
          //  string x = typeComboBox.SelectedItem.ToString;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
         /*   if(configuration == null)
            {
                configuration = new Configuration.Configuration();
            }
            foreach(ILayer layer in mapBox1.Map.Layers)
            {
                if (layer.GetType() == typeof(VectorLayer))
                {
                    VectorLayer vl = (VectorLayer)layer;
                    string layerType;
                    layerTypes.TryGetValue(layer.LayerName, out layerType);

                    switch (layerType)
                    {
                        case "point":
                            Color color1 = (vl.Style.PointColor as SolidBrush).Color;

                            PointLayerConfiguration plc = new PointLayerConfiguration();
                            plc.name = vl.LayerName;
                           // plc.color = color1.Name;
                            plc.symbolUri = "proba";
                           // configuration.pointLayersList.Add(plc);
                            break;

                        case "line":
                            Color color2 = (vl.Style.PointColor as SolidBrush).Color;

                            LineLayerConfiguration llc = new LineLayerConfiguration();
                            llc.name = vl.LayerName;
                          //  llc.color = color2.Name;
                           // llc.type = vl.Style.Line.PenType.ToString();
                          //  llc.width = vl.Style.Line.Width.ToString();
                          //  configuration.lineLayersList.Add(llc);
                            break;

                        case "polygon":

                            break;
                    }

                    //configuration.serializeConfiguration();
                 }
            } */
            
        }

        private void buttonShortestPath_Click(object sender, EventArgs e)
        {
            string connstring = String.Format("Server={0};Port={1};" +
           "User Id={2};Password={3};Database={4};",
           "127.0.0.1", "5432", "postgres",
           "admin", "serbia");

            // Connect to a PostgreSQL database
            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            //pgr_bdDijkstra parametri:
            ////-sql upit koji vraca (sve) ulice
            //////-id - gid ulice
            //////-source - source oznaka za tu ulicu
            //////-target - target oznaka za tu ulicu
            //////-cost - cena ulice
            //////-reverse_cost - obrnuta cena ulice
            ////-source - source atribut ulice koja je pocetak puta (od koje krece rutiranje)
            ////-target - target atribut ulice koja je kraj puta (krajnja tacka rutiranja)
            ////-directed - da li je usmereni graf
            ////-rcost - da li da koristi obrnutu cenu (cena u suprotnom smeru)
            //Dijkstra nam vraca seq, id1, id2, cost (redni broj, node, edge, cena)
            //INNER join sa ways, da bismo za svaki edge znali geometriju, a da pritom zadrzimo
            //redosled ivica; x1, y1, x2, y2 koristimo radi pravilnog iscrtavanja prve i poslednje ivice

            //source i target
            //trazimo najblizu ulicu (liniju) pocetnoj i krajnjoj lokaciji (tacka)
            //koristi se closestpoint(linija, tacka) sto nalazi najblizu tacku u odnosu na neku liniju
            //pa rastojanje od tacke do dobijene tacke na liniji; sortiramo duzine i uzmemo put (liniju)
            //koja je najmanje udaljena od tacke

            string tableNameStart = layerTableNames[fdr1.Table.TableName];
            string tableNameEnd = layerTableNames[fdr2.Table.TableName];

            NpgsqlCommand command = new NpgsqlCommand(
                        "SELECT  dijkstra.id2, dijkstra.seq, x1, y1, x2, y2, geom::bytea FROM" +
                        " pgr_bdDijkstra('SELECT gid AS id, source::int4, target::int4, cost::float8, reverse_cost::float8 FROM public.ways'," +
                        " (select source from" +
                        " (select st_distance(st_closestpoint(r.geom, p.geom)," +
                               " p.geom) as cp," +
                               " r.source," +
                               " r.target" +
                               " from ways r, (select * from " + tableNameStart + " where gid = " + fdr1.ItemArray[0].ToString() + ") as p" +
                               " order by cp asc" +
                               " limit 1) as way1)::int4," +
                        " (select target from" +
                        " (select st_distance(st_closestpoint(r.geom, p.geom)," +
                               " p.geom) as cp," +
                               " r.source," +
                               " r.target" +
                               " from ways r, (select * from " + tableNameEnd + " where gid = " + fdr2.ItemArray[0].ToString() + ") as p" +
                               " order by cp asc" +
                               " limit 1) as way2)::int4," +
                               " true, true)" +
                               " as dijkstra" +
                               " INNER JOIN ways" +
                               " ON dijkstra.id2 = ways.gid",
                               conn); 



            //!!!!!!!!!!
          //  this.layerAttributeListComboBox.Items.Clear();



            //////////////////////////////////

            if (mapBox1.Map.GetLayerByName("Path") != null)
            {
                mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("Path"));
            }

            VectorLayer vl = new VectorLayer("Path");

            Collection<GeoAPI.Geometries.IGeometry> geomColl = new Collection<GeoAPI.Geometries.IGeometry>();
            //Get the default geometry factory
            GeoAPI.GeometryServiceProvider.Instance = new NetTopologySuite.NtsGeometryServices();
            GeoAPI.Geometries.IGeometryFactory gf =
                GeoAPI.GeometryServiceProvider.Instance.CreateGeometryFactory();


            var geoReader = new PostGisReader(gf.CoordinateSequenceFactory, gf.PrecisionModel);

            NpgsqlDataReader dr;
            //iz neobjasnjivih razloga prethodno definisana sql naredba ne vraca ponekad rezultate
            //ideja je da se izvrsi vise puta, dok ne vrati rezultat
            int limit = 0;
                dr = command.ExecuteReader();
            while (!dr.HasRows && limit < 10) //?
            {
                dr = command.ExecuteReader();
                limit++;
            }
            if(limit == 10)
            {
                MessageBox.Show("Routing not possible at the time. Try again.");
                conn.Close();
                return;
            }
            FeatureDataTable fdt = new FeatureDataTable();
            //create header for table from reader
            fdt = CreateTableFromReader(dr, dr.FieldCount - 1);

            int count = 0;
            while (dr.Read())
            {
                IGeometry g = null;
                if (!dr.IsDBNull(dr.FieldCount - 1))
                {
                    //citamo geometriju za svaki red
                    g = geoReader.Read((byte[])dr.GetValue(dr.FieldCount - 1));
                    if (count != 0)//nultu ne dodajemo jer nju ne iscrtavamo celu
                    {
                        geomColl.Add(g);
                    }
                    count++;
                    //this.layerAttributeListComboBox.Items.Add(dr[0]);

                    //filling table with attribute data from rows
                    string[] listOfValues = new string[dr.FieldCount - 1];
                    for (int i = 0; i < dr.FieldCount - 1; i++)
                    {
                        listOfValues[i] = dr[i].ToString();
                    }

                    fdt.Rows.Add(listOfValues);


                    
                }

            }

            count--;

            if(count <= 1)
            {
                MessageBox.Show("Places are just one block apart. :)");
                conn.Close();
                return;
            }

            //nalazimo pocetnu tacku druge ulice (ivice) u putu, i poslednju tacke predposlednje
            //ulice da bismo iscrtali deo prve i poslednje ulice, radi pravilnog prikaza
            Coordinate startSecondStreet1 = null;
            if ((double)fdt[0].ItemArray[2] == (double)fdt[1].ItemArray[4] && //x1 za prvu ulicu, x2 za drugu ulicu
                (double)fdt[0].ItemArray[3] == (double)fdt[1].ItemArray[5])//y1 za prvu, y2 za drugu
            {
                startSecondStreet1 = new Coordinate((double)fdt[0].ItemArray[2], (double)fdt[0].ItemArray[3]);//x1,y1
            }
            else
            {
                startSecondStreet1 = new Coordinate((double)fdt[0].ItemArray[4], (double)fdt[0].ItemArray[5]);//x2,y2
            }

            Coordinate endSecondToLastStreet1 = null;
            if ((double)fdt[count - 1].ItemArray[2] == (double)fdt[count - 2].ItemArray[4] && //x1 za prvu ulicu, x2 za drugu ulicu
                (double)fdt[count - 1].ItemArray[3] == (double)fdt[count - 2].ItemArray[5])//y1 za prvu, y2 za drugu
            {
                endSecondToLastStreet1 = new Coordinate((double)fdt[count - 1].ItemArray[2], (double)fdt[count - 1].ItemArray[3]);//x1,y1
            }
            else
            {
                endSecondToLastStreet1 = new Coordinate((double)fdt[count - 1].ItemArray[4], (double)fdt[count - 1].ItemArray[5]);//x2,y2
            }

            //transformisemo u odgovarajucu projekciju
            var ctFac1 = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory();
            var csSrc1 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;//4326
            var csTgt1 = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator;//3857
            var ct = ctFac1.CreateFromCoordinateSystems(csSrc1, csTgt1);
            var startSecondStreet = ct.MathTransform.Transform(startSecondStreet1);
            var endSecondToLastStreet = ct.MathTransform.Transform(endSecondToLastStreet1);


            //pravimo sql naredbu za selekciju najblizih tacaka na prvoj i posledjoj ulici
            //u odnosu na selektovane objekte (lokacije)
            NpgsqlCommand command1 = new NpgsqlCommand(
                "select point1::bytea, point2::bytea from st_closestpoint((select geom from ways where gid = " + fdt[0].ItemArray[0].ToString() + ")," +
                              " (select geom from " + tableNameStart + " where gid = " + fdr1.ItemArray[0].ToString() + ")) as point1," +
                              " st_closestpoint((select geom from ways where gid = " + fdt[count - 1].ItemArray[0].ToString() + ")," +
                              " (select geom from " + tableNameEnd + " where gid = " + fdr2.ItemArray[0].ToString() + "))  as point2"
                              , conn);
            NpgsqlDataReader dr1 = command1.ExecuteReader();

            dr1.Read();

            ////////!!!!!!!!!!!!!!!!!!!!!!!!!!
            //izbacujemo poslednju ulicu jer se ne iscrtava cela
            geomColl.RemoveAt(count - 1);
            ///////!!!!!!!!!!!!!!!!!!!!!!!!!!!

            //citamo geometrije najblizih tacaka na prvoj i posledjoj ulici
            //u odnosu na selektovane objekte (lokacije)
            IGeometry pointOnStartWay = geoReader.Read((byte[])dr1.GetValue(0));
            IGeometry pointOnEndWay = geoReader.Read((byte[])dr1.GetValue(1));

            //kreiramo linije na osnovu dve tacke (tacke izabrane lokacije i najblize na liniji)
            var lineGeometry1 = new NetTopologySuite.Geometries.LineString(new[] {
                pointOnStartWay.Coordinate,
                fdr1.Geometry.Coordinate
                });

            var lineGeometry2 = new NetTopologySuite.Geometries.LineString(new[] {
                pointOnEndWay.Coordinate,
                fdr2.Geometry.Coordinate
                });
            geomColl.Add(lineGeometry1);
            geomColl.Add(lineGeometry2);




            //dodavaje delova prve i poslednje ulice/////////////////
            //kreiranje linija prve i poslednje ulice na osnovu nadjene najblize tacke
            //i zajednicke tacke za prvu i drugu ulicu (predposlednju i poslednju)
            var lineGeometryStartPart = new NetTopologySuite.Geometries.LineString(new[] {
                pointOnStartWay.Coordinate,
                startSecondStreet
                });

            var lineGeometryEndPart = new NetTopologySuite.Geometries.LineString(new[] {
                endSecondToLastStreet,
                pointOnEndWay.Coordinate                
                });

            geomColl.Add(lineGeometryStartPart);
            geomColl.Add(lineGeometryEndPart);
            ////////////////////////////////////////////////////////

            vl.DataSource = new SharpMap.Data.Providers.GeometryProvider(geomColl);


            vl.Style.Line.Color = Color.Purple;
            vl.Style.Line.Width = 5;
            mapBox1.Map.Layers.Add(vl);
            //loadLayerBackground(vl, "Path");
            mapBox1.Refresh();

            conn.Close();
        }

        private VectorLayer waterwaysLayer;
        private VectorLayer waterLayer;

        private VectorLayer transportLayer;


        private VectorLayer railwaysLayer;

        private VectorLayer landuseLayer;
        private VectorLayer buildingsLayer;

        private VectorLayer waysLayer;

        private VectorLayer pofwLayer;

        private void Form1_Load(object sender, EventArgs e)
        {
            

            pictureBoxTD.ImageLocation = @"./img/logo.png";

            labelTDName.Text = "Welcome to" + Environment.NewLine +
                               "tourist guide" + Environment.NewLine +
                               "for the city" + Environment.NewLine +
                               "of Nis.";

            metroLabel6.Text = "To get things started you can add new layers," + Environment.NewLine +
                               "select features and run queries.";



            //waterways layer////////////////////////////////////////////////
            waterwaysLayer = new VectorLayer("Waterways");
            this.layerTableNames.Add("Waterways", "waterways");
            this.layerTypes.Add("Waterways", "line");
            if (configuration.loadConfiguration("line", "waterways"))
            {
                LineLayerConfiguration llc = new LineLayerConfiguration();
                configuration.lineLayersDictionary.TryGetValue("waterways", out llc);
                waterwaysLayer.Style.Line.Color = llc.color;
                waterwaysLayer.Style.Line.Width = (float)llc.width;
                waterwaysLayer.Style.Line.DashStyle = llc.type;
            }
            else
            {                
                waterwaysLayer.Style.Line.Color = Color.Blue;                
                LineLayerConfiguration llc = new LineLayerConfiguration()
                {
                    color = Color.Blue,
                    name = "waterways",
                    width = 1,
                    type = System.Drawing.Drawing2D.DashStyle.Solid
                };
                configuration.lineLayersDictionary.Add("waterways", llc);
                configuration.saveConfiguration("line", "waterways");
            }
            loadLayerBackground(waterwaysLayer, "waterways");
            /////////////////////////////////////////////////////////////////

            waterLayer = new VectorLayer("Water");
            waterLayer.Style.Fill = new SolidBrush(Color.Blue);
            loadLayerBackground(waterLayer, "water");


            //pois layer/////////////////////////////////////////////////////
            poisLayer = new VectorLayer("Places of Interest");
            layerTypes.Add("Places of Interest", "point");
            layerTableNames.Add("Places of Interest", "pois");
            if (configuration.loadConfiguration("point", "pois"))
            {
                PointLayerConfiguration plc = new PointLayerConfiguration();
                configuration.pointLayersDictionary.TryGetValue("pois", out plc);
                poisLayer.Style.PointColor = new SolidBrush(plc.color);
                if (plc.symbolUri != "none")
                {
                   poisLayer.Style.Symbol = Image.FromFile(plc.symbolUri);
                }
            }
            else
            {

                poisLayer.Style.PointColor = new SolidBrush(Color.Red);
                PointLayerConfiguration plc = new PointLayerConfiguration()
                {
                    color = Color.Red,
                    name = "pois",
                    symbolUri = "none"
                };
                configuration.pointLayersDictionary.Add("pois", plc);
                configuration.saveConfiguration("point", "pois");
            }
            loadLayerBackground(poisLayer, "pois");
            //////////////////////////////////////////////////////////////////
            /////pofw layer/////////////////////////////////////////////////////////
            pofwLayer = new VectorLayer("Places of Worship");
            layerTypes.Add("Places of Worship", "point");
            layerTableNames.Add("Places of Worship", "pofw");
            if (configuration.loadConfiguration("point", "pofw"))
            {
                PointLayerConfiguration plc = new PointLayerConfiguration();
                configuration.pointLayersDictionary.TryGetValue("pofw", out plc);
                pofwLayer.Style.PointColor = new SolidBrush(plc.color);
                if (plc.symbolUri != "none")
                {
                    pofwLayer.Style.Symbol = Image.FromFile(plc.symbolUri);
                }
            }
            else
            {

                pofwLayer.Style.PointColor = new SolidBrush(Color.LightBlue);
                PointLayerConfiguration plc = new PointLayerConfiguration()
                {
                    color = Color.LightBlue,
                    name = "pofw",
                    symbolUri = "none"
                };
                configuration.pointLayersDictionary.Add("pofw", plc);
                configuration.saveConfiguration("point", "pofw");
            }
            loadLayerBackground(pofwLayer, "pofw");

            ////////////////////////////////////////////////////////////////////////
            //transport layer////////////////////////////////////////////////
            transportLayer = new VectorLayer("Transport");
            this.layerTableNames.Add("Transport", "transport");
            this.layerTypes.Add("Transport", "point");
            if (configuration.loadConfiguration("point", "transport"))
            {
                PointLayerConfiguration plc = new PointLayerConfiguration();
                configuration.pointLayersDictionary.TryGetValue("transport", out plc);
                transportLayer.Style.PointColor = new SolidBrush(plc.color);
                if(plc.symbolUri != "none")
                {
                    transportLayer.Style.Symbol = Image.FromFile(plc.symbolUri);
                }
            }
            else
            {

                transportLayer.Style.PointColor = new SolidBrush(Color.Purple);
                PointLayerConfiguration plc = new PointLayerConfiguration()
                {
                    color = Color.Purple,
                    name = "transport",
                    symbolUri = "none"
                };
                configuration.pointLayersDictionary.Add("transport", plc);
                configuration.saveConfiguration("point", "transport");
            }
            loadLayerBackground(transportLayer, "transport");



            
            
            //configuration file for transport
            
            
            /////////////////////////////////////////////////////////////////


            //railways layer////////////////////////////////////////////////
            railwaysLayer = new VectorLayer("Railways");
            this.layerTableNames.Add("Railways", "railways");
            this.layerTypes.Add("Railways", "line");        
            if (configuration.loadConfiguration("line", "railways"))
            {
                LineLayerConfiguration llc = new LineLayerConfiguration();
                configuration.lineLayersDictionary.TryGetValue("railways", out llc);;
                railwaysLayer.Style.Line.Color = llc.color;
                railwaysLayer.Style.Line.Width = (float)llc.width;
                railwaysLayer.Style.Line.DashStyle = llc.type;
            }
            else
            {
                railwaysLayer.Style.Line.Color = Color.Gray;
                railwaysLayer.Style.Line.DashStyle = DashStyle.Dash;
                LineLayerConfiguration llc = new LineLayerConfiguration()
                {
                    color = Color.Gray,
                    name = "railways",
                    width = 1,
                    type = System.Drawing.Drawing2D.DashStyle.Dash
                };
                configuration.lineLayersDictionary.Add("railways", llc);
                configuration.saveConfiguration("line", "railways");
            }
            loadLayerBackground(railwaysLayer, "railways");
            //////////////////////////////////////////////////////////////////////

            buildingsLayer = new VectorLayer("Buildings");
            //buildingsLabelLayer.LabelColumn = "name";
            loadLayerBackground(buildingsLayer, "buildings");
            layerTableNames.Add("Buildings", "buildings");
            layerTypes.Add("Buildings", "point");

            landuseLayer = new VectorLayer("Landuse");
            landuseLayer.Style.Fill = new SolidBrush(Color.Green);
            loadLayerBackground(landuseLayer, "landuse");
            layerTableNames.Add("Landuse", "landuse");
            layerTypes.Add("Landuse", "polygon");


            //ways layer////////////////////////////////////////////////////////////////
            waysLayer = new VectorLayer("Ways");
            layerTableNames.Add("Ways", "ways");
            layerTypes.Add("Ways", "line");
            if (configuration.loadConfiguration("line", "ways"))
            {
                LineLayerConfiguration llc = new LineLayerConfiguration();
                configuration.lineLayersDictionary.TryGetValue("ways", out llc); ;
                waysLayer.Style.Line.Color = llc.color;
                waysLayer.Style.Line.Width = (float)llc.width;
                waysLayer.Style.Line.DashStyle = llc.type;
            }
            else
            {
                waysLayer.Style.Line.Color = Color.Red;
                waysLayer.Style.Line.DashStyle = DashStyle.Solid;
                LineLayerConfiguration llc = new LineLayerConfiguration()
                {
                    color = Color.Red,
                    name = "ways",
                    width = 1,
                    type = System.Drawing.Drawing2D.DashStyle.Solid
                };
                configuration.lineLayersDictionary.Add("ways", llc);
                configuration.saveConfiguration("line", "ways");
            }
            loadLayerBackground(waysLayer, "ways");
            ///////////////////////////////////////////////////////////////////////////



            relationComboBox.Items.Add("Intersects");
            relationComboBox.Items.Add("Difference");
            relationComboBox.Items.Add("Sym Difference");
            relationComboBox.Items.Add("Overlaps");
            relationComboBox.Items.Add("Contains");
            relationComboBox.Items.Add("Crosses");
            relationComboBox.Items.Add("Covered By");
            relationComboBox.Items.Add("Within");
            relationComboBox.Items.Add("Covers");
            relationComboBox.Items.Add("Contains Properly");
            relationComboBox.Items.Add("Contained By");


            relationComboBox.Items.Add("Distance");

            relationComboBox.Items.Add("N-Nearest Objects");

            queryLayer1ComboBox.Items.Add("Select Features");
            queryLayer2ComboBox.Items.Add("Select Features");





            //definisanje stila sloja turistickih podataka kada su selektovani objekti
            SharpMap.Styles.VectorStyle restaurantStyle = new SharpMap.Styles.VectorStyle();
            restaurantStyle.Symbol = Image.FromFile("images/restaurant_white.png");

            SharpMap.Styles.VectorStyle hotelStyle = new SharpMap.Styles.VectorStyle();
            hotelStyle.Symbol = Image.FromFile("images/hotel1_white.png");

            SharpMap.Styles.VectorStyle cinemaStyle = new SharpMap.Styles.VectorStyle();
            cinemaStyle.Symbol = Image.FromFile("images/cinema_white.png");

            SharpMap.Styles.VectorStyle monumentStyle = new SharpMap.Styles.VectorStyle();
            monumentStyle.Symbol = Image.FromFile("images/monument_white.png");

            SharpMap.Styles.VectorStyle coffeeStyle = new SharpMap.Styles.VectorStyle();
            coffeeStyle.Symbol = Image.FromFile("images/coffee_white.png");

            SharpMap.Styles.VectorStyle cultureStyle = new SharpMap.Styles.VectorStyle();
            cultureStyle.Symbol = Image.FromFile("images/culture_white.png");

            SharpMap.Styles.VectorStyle defaultTourDataStyle = new SharpMap.Styles.VectorStyle();
            defaultTourDataStyle.PointColor = new SolidBrush(Color.White);

            //Create the theme items
            Dictionary<string, SharpMap.Styles.IStyle> styles = new Dictionary<string, SharpMap.Styles.IStyle>();
            styles.Add("restaurant", restaurantStyle);
            styles.Add("hotel", hotelStyle);
            styles.Add("cinema", cinemaStyle);
            styles.Add("monument", monumentStyle);
            styles.Add("coffeepub", coffeeStyle);
            styles.Add("culture", cultureStyle);

            //Assign the theme
            SharpMap.Rendering.Thematics.UniqueValuesTheme<string> tourDataSelectedTheme = new SharpMap.Rendering.Thematics.UniqueValuesTheme<string>("type", styles, defaultTourDataStyle);
            _tourDataSelectedTheme = tourDataSelectedTheme;


            //dodavanje operatora
            operatorsComboBox1.Items.Add("=");
            operatorsComboBox1.Items.Add("!=");
            operatorsComboBox1.Items.Add("<");
            operatorsComboBox1.Items.Add("<=");
            operatorsComboBox1.Items.Add(">");
            operatorsComboBox1.Items.Add(">=");

            operatorsComboBox1.Items.Add("LIKE");
            operatorsComboBox1.Items.Add("NOT LIKE");


            //dodavanje itema za combo box za klijentski domen
            clientLayerSelectComboBox.Items.Add("Tourist locations");
            clientLayerSelectComboBox.Items.Add("Transport");
            clientLayerSelectComboBox.Items.Add("Landuse");
            clientLayerSelectComboBox.Items.Add("Places of Interest");



            //popunjavacnje comboboxa koji se koristi za pretragu objekata po imenu,
            //radi preporuka
            addSuggestions();


            //popuna combo box-ova za deo za filtriranje po vremenu
            for(int i =0; i<24; i++)
            {
                for(int j=0; j<2; j++)
                {
                    comboBox1.Items.Add((i + j * 0.3).ToString());
                    comboBox2.Items.Add((i + j * 0.3).ToString());
                }
              
            }
            comboBox1.Items.Add((24).ToString());
            comboBox2.Items.Add((24).ToString());

        }
        private SharpMap.Rendering.Thematics.UniqueValuesTheme<string> _tourDataSelectedTheme = null;

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            //mapBox1.Invalidate();
            MessageBox.Show(mapBox1.Map.Zoom.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
   
            
        }

        private void mapBox1_GeometryDefined(IGeometry geometry)
        {
            //  mapQueryToolStrip1.Items
            // string selectedLayer = comboBoxSelectLayer.SelectedItem.ToString();
            string selectedLayer = mapQueryToolStrip1._queryLayerPicker.SelectedItem.ToString();
            VectorLayer vl = (VectorLayer)mapBox1.Map.GetLayerByName(selectedLayer);

            FeatureDataSet fds = new FeatureDataSet();
            vl.ExecuteIntersectionQuery(geometry, fds);

            this.mapBox1_MapQueried(fds.Tables[0]);

            using (FeaturesGridViewForm fgvf = new FeaturesGridViewForm(fds.Tables[0]))
            {
                //fgvf.ShowDialog();
            }


        }

        private void buttonExecuteRelation_Click(object sender, EventArgs e)
        {
            if(relationComboBox.SelectedItem == null)
            {
                MessageBox.Show("Select relation.");
                return;
            }
            string connstring = String.Format("Server={0};Port={1};" +
                "User Id={2};Password={3};Database={4};",
                "127.0.0.1", "5432", "postgres",
                "admin", "serbia");

            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();
            NpgsqlCommand command = null;

            string layerNameToShow = null;


            ///////////sa leve objekat/grupa objekata; sa desne objekat/grupa obejkata//////////////////////////////////////////////////////////////////////////////////////////
            if ((string)queryLayer1ComboBox.SelectedItem == "SelectedFeatures1(Blue)" 
                && (string)queryLayer2ComboBox.SelectedItem == "SelectedFeatures2(Green)")
            {
                string selectedFeatures1 = (string)queryLayer1ComboBox.SelectedItem;
                string selectedFeatures2 = (string)queryLayer2ComboBox.SelectedItem;
                

                DataRowCollection rows1 = _selectedFeaturesTable1.Rows;
                DataRowCollection rows2 = _selectedFeaturesTable2.Rows;
                string gidsForTable1 = "";
                string gidsForTable2 = "";

                foreach (DataRow drow in rows1)
                {
                    gidsForTable1 += drow[0].ToString() + ",";
                }

                foreach (DataRow drow in rows2)
                {
                    gidsForTable2 += drow[0].ToString() + ",";
                }

                gidsForTable1 = gidsForTable1.Remove(gidsForTable1.Length - 1);
                gidsForTable2 = gidsForTable2.Remove(gidsForTable2.Length - 1);

                string table1 = layerTableNames[_selectedFeaturesTable1.TableName];
                string table2 = layerTableNames[_selectedFeaturesTable2.TableName];


                layerNameToShow = table1;

                switch (relationComboBox.SelectedItem.ToString())
                {
                    case "Intersects":
                        command =
                        new NpgsqlCommand(
                        "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                        " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                        " (select * from " + table2 + " where gid in ("
                        + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                        + " where ST_Intersects(" + table1 + "1.geom, "
                        + table2 + "2.geom)", conn);

                        break;
                   case "Difference":
                       
                        command =
                         new NpgsqlCommand(
                         "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " +
                         " COALESCE(ST_Difference(" + table1 + "1.geom, " + table2 + "2.geom), " + table1 + "1.geom)::bytea FROM" +
                         " (select * from " + table1 + " where gid in ("
                         + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                         " (select * from " + table2 + " where gid in ("
                         + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                         + " where ST_Intersects(" + table1 + "1.geom, "
                         + table2 + "2.geom)", conn);
                        //////////////////////////////////////////////////////////     

                        break;

                    case "Contains":

                        command =
                        new NpgsqlCommand(
                        "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                        " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                        " (select * from " + table2 + " where gid in ("
                        + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                        + " where ST_Contains(" + table1 + "1.geom, "
                        + table2 + "2.geom)", conn);


                        break;

                   case "Crosses":

                        command =
                        new NpgsqlCommand(
                        "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                        " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                        " (select * from " + table2 + " where gid in ("
                        + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                        + " where ST_Crosses(" + table1 + "1.geom, "
                        + table2 + "2.geom)", conn);



                        break;

                         case "Covered By":

                        command =
                 new NpgsqlCommand(
                 "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                 " (select * from " + table1 + " where gid in ("
                 + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                 " (select * from " + table2 + " where gid in ("
                 + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                 + " where ST_CoveredBy(" + table1 + "1.geom, "
                 + table2 + "2.geom)", conn);



                        break;

                              case "Within":
                        command =
           new NpgsqlCommand(
           "select distinct on (" + table1 + "1.gid) "  + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
           " (select * from " + table1 + " where gid in ("
           + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
           " (select * from " + table2 + " where gid in ("
           + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
           + " where ST_Within(" + table1 + "1.geom, "
           + table2 + "2.geom)", conn);

                        break;

                          case "Covers"://////?????????
                        command =
                         new NpgsqlCommand(
                         "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                         " (select * from " + table1 + " where gid in ("
                         + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                         " (select * from " + table2 + " where gid in ("
                         + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                         + " where ST_Covers(" + table1 + "1.geom, "
                         + table2 + "2.geom)", conn);

                        break;

                            case "Contains Properly":
                        command =
                        new NpgsqlCommand(
                        "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                        " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                        " (select * from " + table2 + " where gid in ("
                        + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                        + " where ST_ContainsProperly(" + table1 + "1.geom, "
                        + table2 + "2.geom)", conn);

                        break;

                    case "Distance":
                        //udaljenost izmdju dva selektovana objekta
                        command = new NpgsqlCommand(
                       "select ST_Distance(" + table1 + "1.geom," + table2 + "2.geom) AS distance from" +
                        " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," +
                        " (select * from " + table2 + " where gid in ("
                        + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2", conn);



                        using (NpgsqlDataReader dr1 = command.ExecuteReader())
                        {
                            dr1.Read();
                            double distance;
                            Double.TryParse(dr1.GetValue(0).ToString(), out distance);
                            distance = Math.Round(distance);
                            metroLabel17.Text = "Distance betwen two selected objects is: " + distance.ToString() + "m";
                        }
                            
                        conn.Close();
                        return;

                        break;
                    case "N-Nearest Objects":
                        MessageBox.Show("Not available for this types of operands.");

                        conn.Close();
                        return;
                        break;
                }



              //  conn.Close();
               // return;
            }
            //////////////////////////////////////////////////////////////////////////////////////////
            ///////////sa leve sloj; sa desne sloj//////////////////////////////////////////////////////////////////////////////////////////
            else if ((string)queryLayer1ComboBox.SelectedItem != "SelectedFeatures1(Blue)"
                && (string)queryLayer2ComboBox.SelectedItem != "SelectedFeatures2(Green)")
            {
                string layerLeft = queryLayer1ComboBox.SelectedItem.ToString();
                string layerRight = queryLayer2ComboBox.SelectedItem.ToString();

                string layerLeftName;
                string layerRightName;
                layerTableNames.TryGetValue(layerLeft, out layerLeftName);
                layerTableNames.TryGetValue(layerRight, out layerRightName);

                layerNameToShow = layerLeftName;



                switch (relationComboBox.SelectedItem.ToString())
                {

                    case "Intersects":
                        command =
                            new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + layerLeftName + ".geom::bytea  "
                             + "FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ","
                             + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + " WHERE ST_Intersects(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);
                        break;
                    case "Difference":
                       

                        command =
                            new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + "COALESCE(ST_Difference(" + layerLeftName + ".geom, " + layerRightName + ".geom), " + layerLeftName + ".geom)::bytea "
                        + "FROM(SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName
                        + " LEFT JOIN(SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                        + " ON ST_Intersects(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);

    

                        break;
 
                    case "Contains":

                        command =
                            new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + layerLeftName + ".geom::bytea  "
                             + "FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ","
                             + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + " WHERE ST_Contains(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);


                        break;

                    case "Crosses":

                        command =
                            new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + layerLeftName + ".geom::bytea "
                             + "FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ","
                             + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + " WHERE ST_Crosses(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);



                        break;

                    case "Covered By":

                        command =
                          new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + layerLeftName + ".geom::bytea "
                           + "FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ","
                           + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                           + " WHERE ST_CoveredBy(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);



                        break;

                    case "Within":
                        command =
                                              new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + layerLeftName + ".geom::bytea "
                                               + "FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ","
                                               + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                                               + " WHERE ST_Within(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);

                        break;

                    case "Covers":
                        command =
                                              new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + layerLeftName + ".geom::bytea "
                                               + "FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ","
                                               + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                                               + " WHERE ST_Covers(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);

                        break;

                    case "Contains Properly":
                        command =
                                                              new NpgsqlCommand("SELECT DISTINCT ON (" + layerLeftName + ".gid) " + layerLeftName + ".*, " + layerLeftName + ".geom::bytea "
                                                               + "FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ","
                                                               + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                                                               + " WHERE ST_ContainsProperly(" + layerLeftName + ".geom, " + layerRightName + ".geom)", conn);

                        break;

                    case "Distance":

                        if (distanceTextBox.Text == "")
                        {
                            MessageBox.Show("Set distance.");
                                conn.Close();
                                return;
                        }
                          command = new NpgsqlCommand(
                              "SELECT DISTINCT ON (" + layerRightName + ".gid) " + layerRightName + ".gid AS id2, " + layerRightName + ".*, ST_Distance(" + layerLeftName + ".geom," + layerRightName + ".geom) AS distance, " + layerRightName + ".geom::bytea" +
                              " FROM (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + ", "
                              + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName +
                              " WHERE ST_DWithin(" + layerLeftName + ".geom, " + layerRightName + ".geom, " + distanceTextBox.Text + ")", conn);

                        layerNameToShow = layerRightName;

                        break;
                    case "N-Nearest Objects":
                        MessageBox.Show("Not available for this types of operands.");

                        conn.Close();
                        return;
                        break;

                }
            }
            //////////////////////////////////////////////////////////////////////////////////////////
            ///////sa leve strane objekat/grupa objekata; sa desne sloj/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            else if ((string)queryLayer1ComboBox.SelectedItem == "SelectedFeatures1(Blue)"
                && (string)queryLayer2ComboBox.SelectedItem != "SelectedFeatures2(Green)")
            {

                //sa leve objekat/grupa objekata
                string selectedFeatures1 = (string)queryLayer1ComboBox.SelectedItem;

                DataRowCollection rows1 = _selectedFeaturesTable1.Rows;
                string gidsForTable1 = "";

                foreach (DataRow drow in rows1)
                {
                    gidsForTable1 += drow[0].ToString() + ",";
                }

                gidsForTable1 = gidsForTable1.Remove(gidsForTable1.Length - 1);

                string table1 = layerTableNames[_selectedFeaturesTable1.TableName];

                //sa desne sloj
                string layerRight = queryLayer2ComboBox.SelectedItem.ToString();
                string layerRightName;
                layerTableNames.TryGetValue(layerRight, out layerRightName);

                layerNameToShow = table1;

                switch (relationComboBox.SelectedItem.ToString()) {
                    case "Intersects"://ok
                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_Intersects(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);
                        break;

                    case "Difference":
                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " +
                            " COALESCE(ST_Difference(" + table1 + "1.geom, " + layerRightName + "2.geom), " + table1 + "1.geom)::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_Intersects(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);
                        break;

                    case "Contains"://ok
                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_Contains(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);
                        break;

                    case "Crosses": //ok                    

                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1," 
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_Crosses(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);


                        break;

                    case "Covered By"://ok
                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_CoveredBy(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);
                        break;

                    case "Within"://ok
                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_Within(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);
                        break;

                    case "Covers"://ok
                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_Covers(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);
                        break;

                    case "Contains Properly"://ok
                        command =
                            new NpgsqlCommand(
                            "select distinct on (" + table1 + "1.gid) " + table1 + "1.*, " + table1 + "1.geom::bytea FROM" +
                            " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                             + "2 WHERE ST_ContainsProperly(" + table1 + "1.geom, " + layerRightName + "2.geom)", conn);
                        break;

                   case "N-Nearest Objects"://ok
                        if(distanceTextBox.Text == "")
                        {
                            MessageBox.Show("Set number of objects.");

                            conn.Close();
                            return;
                        }
                        command =
                            new NpgsqlCommand(
                        "select " + layerRightName + "2.*, ST_Distance(" + table1 + "1.geom, " + layerRightName + "2.geom) as distance, " + layerRightName + "2.geom::bytea" +
                        " from " +
                        " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                        + "2 order by distance limit " + distanceTextBox.Text, conn);

                        layerNameToShow = layerRightName;
                        break;
                    case "Distance":
                        MessageBox.Show("This relation not available for this types of operands");

                        conn.Close();
                        return;
                        break;
                }
            }
            //////////////////////////////////////////////////////////////////////////////////////////
            ///////sa leve strane sloj; sa desne objekat/grupa objekata/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            else if ((string)queryLayer1ComboBox.SelectedItem != "SelectedFeatures1(Blue)"
                && (string)queryLayer2ComboBox.SelectedItem == "SelectedFeatures2(Green)")
            {
                //sa leve sloj
                string layerLeft = queryLayer1ComboBox.SelectedItem.ToString();
                string layerLeftName;
                layerTableNames.TryGetValue(layerLeft, out layerLeftName);

                //sa desne objekat/ grupa objekata
                string selectedFeatures2 = (string)queryLayer2ComboBox.SelectedItem;

                DataRowCollection rows2 = _selectedFeaturesTable2.Rows;
                string gidsForTable2 = "";

                foreach (DataRow drow in rows2)
                {
                    gidsForTable2 += drow[0].ToString() + ",";
                }

                gidsForTable2 = gidsForTable2.Remove(gidsForTable2.Length - 1);

                string table2 = layerTableNames[_selectedFeaturesTable2.TableName];

                

                switch (relationComboBox.SelectedItem.ToString())
                {
                    case "Intersects"://ok

                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_Intersects(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                    case "Difference"://ok

                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + " COALESCE(ST_Difference(" + layerLeftName + "1.geom, " + table2 + "2.geom), " + layerLeftName + "1.geom)::bytea FROM " +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_Intersects(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                    case "Contains":
                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_Contains(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                    case "Crosses"://ok

                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_Crosses(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;



                    case "Overlaps":////////!!!!!!!!!!!!!!!!!!!!!!!!!!
                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_Overlaps(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                  


                    case "Covered By":
                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_CoveredBy(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                    case "Within":
                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_Within(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                    case "Covers":
                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_Covers(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                    case "Contains Properly":
                        command =
                           new NpgsqlCommand(
                           "select distinct on (" + layerLeftName + "1.gid) " + layerLeftName + "1.*, " + layerLeftName + "1.geom::bytea FROM" +
                           " (SELECT * FROM " + layerLeftName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerLeftName + "1," +
                           " (select * from " + table2 + " where gid in ("
                       + gidsForTable2 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table2 + "2"
                            + " WHERE ST_ContainsProperly(" + layerLeftName + "1.geom, " + table2 + "2.geom)", conn);
                        break;

                    case "Distance":
                        MessageBox.Show("Relation not possible betwen this two type of operands.");
                        conn.Close();
                        return;
                        break;
                    case "N-Nearest Objects":
                        MessageBox.Show("Relation not possible betwen this two type of operands.");
                        conn.Close();
                        return;
                        break;
                }
            }
            //////////////////////////////////////////////////////////////////////////////////////////
            else
            {
                MessageBox.Show("Select again parameters of relation.");
            }


            // Define a query


            this.addQueriedFeaturesToMap(command, layerNameToShow);

            

            conn.Close();

        }

        public FeatureDataTable CreateTableFromReader(System.Data.Common.DbDataReader reader, int geomIndex)
        {
            var res = new FeatureDataTable { TableName = _queriedLayerName };
            for (var c = 0; c < geomIndex; c++)
            {
                var fieldType = reader.GetFieldType(c);
                if (fieldType == null)
                    throw new Exception("Unable to retrieve field type for column " + c);
                res.Columns.Add(reader.GetName(c), fieldType);
            }
            return res;
        }

        private void enableSpatialQueriesCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxRoutingEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBoxRoutingEnable.Checked)
            {
                fdr1 = null;
                fdr2 = null;
            }
        }

        private void buttonClearSelectionRouting_Click(object sender, EventArgs e)
        {
            fdr1 = null;
            fdr2 = null;

            routeEndTextBox.Text = "";
            routeStartTextBox.Text = "";

            if(mapBox1.Map.GetLayerByName("Path") != null)

            mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("Path"));
            mapBox1.Refresh();
        }

        private void buttonShowInTable_Click(object sender, EventArgs e)
        {
            using (FeaturesGridViewForm form = new FeaturesGridViewForm(_data))
            {
                form.ShowDialog();
            }
        }

        private void linkTDWebsite_Click(object sender, EventArgs e)
        {
         //   linkTDWebsite.vis = true;
            System.Diagnostics.Process.Start(_data[0].ItemArray[8].ToString());
        }

        //obrisati
        private void typeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
           // string x = typeComboBox.SelectedItem.ToString();
        }

        private void typeComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            string typeValue = typeComboBox.SelectedItem.ToString();

            if (typeValue == "All")
            {
                if (mapBox1.Map.GetLayerByName("QueriedFeatures") != null)
                {
                    mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("QueriedFeatures"));
                    checkedListBox1.Items.Remove("QueriedFeatures");
                    mapBox1.Refresh();
                }
                return;
            }

            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            // Define a query
            NpgsqlCommand command = null;
                command = new NpgsqlCommand("select tour_data.*, tour_data.geom::bytea from tour_data where type='" + 
                    typeValue + "'", conn);

            this.addQueriedFeaturesToMap(command, "Tourist locations");

         

            conn.Close();




        }

      
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if((this._layerDataInTree == "Tourist locations" || 
                this._layerDataInTree == "tour_data")
                && e.Node.Level == 0)
            {
                DataRow data = _data.Rows[e.Node.Index];
                metroLabel6.Text = "";
                labelTDName.Text = data.ItemArray[3].ToString();
                labelTDType.Text = data.ItemArray[2].ToString();
                labelTDDescription.Text = data.ItemArray[4].ToString();
                labelTDOpenFrom.Text = data.ItemArray[5].ToString();
                labelTDOpenTo.Text = data.ItemArray[6].ToString();

                pictureBoxTD.LoadAsync(data.ItemArray[7].ToString());

                
            }


           //
        }

        private List<string> getAttributesForLayer(string layerName)
        {
            //select distinct on(tour_data.type) tour_data.type
            //from tour_data


            //select distinct on(landuse.fclass) landuse.fclass
            //from(select * from landuse where geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))
            //as landuse


            string selectedLayerTableName;
            this.layerTableNames.TryGetValue(layerName, out selectedLayerTableName);

            string connstring = String.Format("Server={0};Port={1};" +
           "User Id={2};Password={3};Database={4};",
           "127.0.0.1", "5432", "postgres",
           "admin", "serbia");

            // Connect to a PostgreSQL database
            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            // Define a query
            NpgsqlCommand command = null;
                
            if(layerName == "Tourist locations")
            {
                command = new NpgsqlCommand("select distinct on(tour_data.type) tour_data.type from tour_data", conn);
            }
            else
            {
               command = new NpgsqlCommand(
                   "select distinct on(" + selectedLayerTableName + ".fclass) " + selectedLayerTableName + ".fclass" +
                   " from (select * from " + selectedLayerTableName + " where geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + selectedLayerTableName, conn);
            }
                

            // Execute the query and obtain a result set
            NpgsqlDataReader dr = command.ExecuteReader();

            List<string> retArray = new List<string>(); 

           // this.clientAttributeSelectComboBox.Items.Clear();
            // Output rows
            while (dr.Read())
                retArray.Add(dr[0].ToString());
                //this.clientAttributeSelectComboBox.Items.Add(dr[0]);

            conn.Close();

            return retArray;
        }

        private void clientLayerSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<string> list = this.getAttributesForLayer(clientLayerSelectComboBox.SelectedItem.ToString());

            clientAttributeSelectComboBox.Items.Clear();
            clientAttributeSelectComboBox.Items.AddRange(list.ToArray());
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            var x = _data; 

            if(_data == null || _data.TableName != "Tourist locations")
            {
                MessageBox.Show("Please select a tourist location.");
                return;
            }

            string connstring = String.Format("Server={0};Port={1};" +
                "User Id={2};Password={3};Database={4};",
                "127.0.0.1", "5432", "postgres",
                "admin", "serbia");

            // Connect to a PostgreSQL database
            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();
            NpgsqlCommand command = null;


            //sa leve objekat/grupa objekata
            DataRowCollection rows1 = _data.Rows;
            string gidsForTable1 = "";

            foreach (DataRow drow in rows1)
            {
                gidsForTable1 += drow[0].ToString() + ",";
            }

            gidsForTable1 = gidsForTable1.Remove(gidsForTable1.Length - 1);

            string table1 = "tour_data";

            //sa desne sloj
            string layerRight = clientLayerSelectComboBox.SelectedItem.ToString();
            string layerRightName;
            layerTableNames.TryGetValue(layerRight, out layerRightName);

            string attr = null;
            if(layerRight == "Tourist locations")
            {
                attr = "type";
            }
            else
            {
                attr = "fclass";
            }

            if(radioButtonNNearest.Checked)
            {
                command =
                            new NpgsqlCommand(
                        "select " + layerRightName + "2.*, ST_Distance(" + table1 + "1.geom, " + layerRightName + "2.geom) as distance, " + layerRightName + "2.geom::bytea" +
                        " from " +
                        " (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                        + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName
                        + "2 where " + layerRightName + "2." + attr + "='" + clientAttributeSelectComboBox.SelectedItem.ToString() + "' order by distance limit " + clientDistanceNumberComboBox.Text, conn);

                _queriedLayerName = clientLayerSelectComboBox.SelectedItem.ToString();
                this.addQueriedFeaturesToMap(command, _queriedLayerName);
            }
            else
            {

                command = new NpgsqlCommand(
                              "SELECT DISTINCT ON (" + table1 + "1.gid) " + table1 + "1.gid AS id2, " + table1 + "1.*, ST_Distance(" + table1 + "1.geom," + layerRightName + "2.geom) AS distance, " + layerRightName + "2.geom::bytea" +
                              " FROM (select * from " + table1 + " where gid in ("
                        + gidsForTable1 + ") and (geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857))) as " + table1 + "1,"
                              + " (SELECT * FROM " + layerRightName + " WHERE geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + layerRightName +
                              "2 WHERE " + layerRightName + "2." + attr + "='" + clientAttributeSelectComboBox.SelectedItem.ToString() + "' and ST_DWithin(" + table1 + "1.geom, " + layerRightName + "2.geom, " + clientDistanceNumberComboBox.Text + ")", conn);

                _queriedLayerName = clientLayerSelectComboBox.SelectedItem.ToString();
                this.addQueriedFeaturesToMap(command, _queriedLayerName);
            }
            

            

            

            conn.Close();

        }

        private void addQueriedFeaturesToMap(NpgsqlCommand command, string layerName)
        {
            _queriedLayerName = layerName;
            ILayer layer = mapBox1.Map.GetLayerByName("QueriedFeatures");
            if (layer != null)
            {
                mapBox1.Map.Layers.Remove(layer);
                checkedListBox1.Items.Remove("QueriedFeatures");
            }

            VectorLayer vlay2 = new VectorLayer("QueriedFeatures");
            Collection<GeoAPI.Geometries.IGeometry> geomColl = new Collection<GeoAPI.Geometries.IGeometry>();
            //Get the default geometry factory
            GeoAPI.GeometryServiceProvider.Instance = new NetTopologySuite.NtsGeometryServices();
            GeoAPI.Geometries.IGeometryFactory gf =
                GeoAPI.GeometryServiceProvider.Instance.CreateGeometryFactory();


            var geoReader = new PostGisReader(gf.CoordinateSequenceFactory, gf.PrecisionModel);

            //NpgsqlCommand command = null;



            if (command != null)
            {
                // Execute the query and obtain a result set
                NpgsqlDataReader dr = command.ExecuteReader();

                FeatureDataTable fdt = new FeatureDataTable();
                fdt = CreateTableFromReader(dr, dr.FieldCount - 1);

                int index = 0;
                while (dr.Read())
                {

                    IGeometry g = null;
                    if (!dr.IsDBNull(dr.FieldCount - 1))
                    {
                        g = geoReader.Read((byte[])dr.GetValue(dr.FieldCount - 1));
                        geomColl.Add(g);
                        //this.layerAttributeListComboBox.Items.Add(dr[0]);

                        string[] listOfValues = new string[dr.FieldCount - 1];
                        for (int i = 0; i < dr.FieldCount - 1; i++)
                        {
                            listOfValues[i] = dr[i].ToString();
                        }                         

                        fdt.Rows.Add(listOfValues);
                        fdt[index++].Geometry = g;


                    }

                }


                using (FeaturesGridViewForm fgvf = new FeaturesGridViewForm(fdt))
                {
                  //  fgvf.ShowDialog();
                }



                mapBox1_MapQueried(fdt);
            }
        }

        

        public void addSuggestions()
        {
            string connstring = String.Format("Server={0};Port={1};" +
                "User Id={2};Password={3};Database={4};",
                "127.0.0.1", "5432", "postgres",
                "admin", "serbia");

            // Connect to a PostgreSQL database
            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();
            NpgsqlCommand command = null;


            command = new NpgsqlCommand("select tour_data.name from tour_data", conn);

            NpgsqlDataReader dr = command.ExecuteReader();

            FeatureDataTable fdt = new FeatureDataTable();
            fdt = CreateTableFromReader(dr, dr.FieldCount - 1);

            while (dr.Read())
            {


                searchComboBox.Items.Add(dr.GetValue(0));
 
            
            }


                    conn.Close();
        }

        private void searchComboBox_Enter(object sender, EventArgs e)
        {
           // int x = 5;
        }

        private void searchComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyValue == 13)
            {
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                NpgsqlCommand command = null;
                command = new NpgsqlCommand("select tour_data.*, tour_data.geom::bytea from tour_data where LOWER(name) like '%" +  
                    searchComboBox.Text.ToLower() + "%'", conn);

                this.addQueriedFeaturesToMap(command, "Tourist locations");



                conn.Close();
            }
        }

        private void buttonFindByTime_Click(object sender, EventArgs e)
        {
            string startTime = comboBox1.SelectedItem.ToString();
            string endTime = comboBox2.SelectedItem.ToString();

            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            NpgsqlCommand command = null;
            command = new NpgsqlCommand("select tour_data.*, tour_data.geom::bytea from tour_data where " +
                "tour_data.openfrom <= " + startTime + " and tour_data.opento >= " + endTime + "", conn);

            this.addQueriedFeaturesToMap(command, "Tourist locations");



            conn.Close();
        }

        private void comboBoxValue_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void layerAttributeListComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedLayerTableName = null;
            this.layerTableNames.TryGetValue(layerListComboBox.SelectedItem.ToString(), out selectedLayerTableName);

            // Connect to a PostgreSQL database
            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            // Define a query
            NpgsqlCommand command = null;


            if(selectedLayerTableName!=null)
            {
                command = new NpgsqlCommand(
                    "select distinct on(" + selectedLayerTableName + "." + layerAttributeListComboBox.SelectedItem.ToString()  + ") " + selectedLayerTableName + "." + layerAttributeListComboBox.SelectedItem.ToString() + " " +
                    " from (select * from " + selectedLayerTableName + " where geom && ST_MakeEnvelope(2432941.12728156, 5365084.24313421, 2441195.1335654, 5357805.54171105, 3857)) as " + selectedLayerTableName, conn);
            }


            // Execute the query and obtain a result set
            NpgsqlDataReader dr = command.ExecuteReader();

            List<string> retArray = new List<string>();

            // this.clientAttributeSelectComboBox.Items.Clear();
            // Output rows
            while (dr.Read())
                retArray.Add(dr[0].ToString());
            //this.clientAttributeSelectComboBox.Items.Add(dr[0]);

            conn.Close();


            comboBoxValue.Items.Clear();
            comboBoxValue.Items.AddRange(retArray.ToArray());
        }

        private void buttonClearSpatial_Click(object sender, EventArgs e)
        {
            if(mapBox1.Map.GetLayerByName("QueriedFeatures")!=null)
            mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("QueriedFeatures"));

            if (mapBox1.Map.GetLayerByName("SelectedFeatures1(Blue)") != null)
                mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("SelectedFeatures1(Blue)"));

            if (mapBox1.Map.GetLayerByName("SelectedFeatures2(Green)") != null)
                mapBox1.Map.Layers.Remove(mapBox1.Map.GetLayerByName("SelectedFeatures2(Green)"));


            if (checkedListBox1.Items.Contains("QueriedFeatures"))
                checkedListBox1.Items.Remove("QueriedFeatures");

            if (checkedListBox1.Items.Contains("SelectedFeatures1(Blue)"))
                checkedListBox1.Items.Remove("SelectedFeatures1(Blue)");

            if (checkedListBox1.Items.Contains("SelectedFeatures2(Green)"))
                checkedListBox1.Items.Remove("SelectedFeatures2(Green)");

            mapBox1.Refresh();

            queryLayer1ComboBox.SelectedItem = null;
            queryLayer2ComboBox.SelectedItem = null;

            relationComboBox.SelectedItem = null;

            distanceTextBox.Text = "";

            _selectedFeaturesTable1 = null;
            _selectedFeaturesTable2 = null;

            metroLabel17.Text = "";
        }




        /////////////////////////////////////////
    }

}

