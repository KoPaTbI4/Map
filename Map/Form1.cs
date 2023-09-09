using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace Map
{ 
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();
            _gMapControl.Dock = DockStyle.Fill;
            _gMapControl.MouseUp += _gMapControl_MouseUp;
            _gMapControl.MouseDown += _gMapControl_MouseDown;
            _gMapControl.MouseMove += _gMapControl_MouseMove;
        }

        private GMapMarker _selectedMarker;

        public struct Markers 
        {
            public int id;
            public double x;
            public double y;
            public string title;
            public string type;
        }

        List<Markers> _markers = new List<Markers>();

        int load_markers()
        {
            try
            {
                string connectionString = @"Data Source=NIKOLAY;Initial Catalog=MarkersDB;Integrated Security=True";
                string query = "SELECT [ID],[Coordinates],[Title] FROM[dbo].[Markers]";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Markers _marker = new Markers();

                        _marker.id = reader.GetInt32(0);

                        string[] coord = reader.GetString(1).Split(',');

                        _marker.x = Convert.ToDouble(coord[0].Replace(".", ","));
                        _marker.y = Convert.ToDouble(coord[1].Replace(".", ","));
                        _marker.title = reader.GetString(2);
                        _marker.type = "old";

                        _markers.Add(_marker);

                        AddMarker(_marker.x, _marker.y, _marker.title);
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            { 
                return -1;
            }
            return 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int ifail = load_markers();

            if(ifail == -1)
            {
                MessageBox.Show("Ошибка работы с базой данных!", "Ошибка", MessageBoxButtons.OK);
                this.Close();
            }
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
           
            _gMapControl.MapProvider = GMapProviders.GoogleMap;
            _gMapControl.Position = new PointLatLng(_markers[0].x, _markers[0].y); 
            _gMapControl.MinZoom = 1;
            _gMapControl.MaxZoom = 18;
            _gMapControl.Zoom = 12;
            _gMapControl.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            _gMapControl.CanDragMap = true;
            _gMapControl.DragButton = MouseButtons.Left;
            _gMapControl.ShowCenter = false;
            _gMapControl.ShowTileGridLines = false;

            
        }

        private void AddMarker(double lat, double lng, string title)
        {
            GMapMarker _marker = new GMarkerGoogle(new PointLatLng(lat, lng), GMarkerGoogleType.red);
            _marker.ToolTipText = "\n" + title;
            GMapOverlay markersOverlay = new GMapOverlay("Markers");
            markersOverlay.Markers.Add(_marker);
            _gMapControl.Overlays.Add(markersOverlay);

            _gMapControl.Update();
        }

        bool checkTitle(string newtitle)
        {
            foreach (Markers _marker in _markers)
            {
                if (_marker.title == newtitle)
                {
                    return true;
                }
            }
            return false;
        }

        private void b_addMarker_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Coord_X.Text) && string.IsNullOrEmpty(Coord_Y.Text) && string.IsNullOrEmpty(Title.Text))
            {
                MessageBox.Show("Необходимые поля не заполнены!");
            }
            else if (checkTitle(Title.Text))
            {
                MessageBox.Show("Введенное имя метки уже занято!");
            }
            else
            {
                Coord_X.Text = Coord_X.Text.Replace(".", ",");
                Coord_Y.Text = Coord_Y.Text.Replace(".", ",");

                Markers _marker = new Markers();

                _marker.x = Convert.ToDouble(Coord_X.Text);
                _marker.y = Convert.ToDouble(Coord_Y.Text);
                _marker.title = Title.Text;
                _marker.type = "new";

                _markers.Add(_marker);

                AddMarker(Convert.ToDouble(Coord_X.Text), Convert.ToDouble(Coord_Y.Text), Title.Text);

                insertDB();
            }
        }

        void updateDB()
        {
            string connectionString = @"Data Source=NIKOLAY;Initial Catalog=MarkersDB;Integrated Security=True";
            string query = "";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                for (int i = 0; i < _markers.Count(); i++)
                {
                    if (_markers[i].type == "update")
                    {
                        Markers _marker = _markers[i];
                        query = "UPDATE [dbo].[Markers] SET ";
                        query += "[Coordinates] = '" + Convert.ToString(_marker.x).Replace(",", ".") + "," + Convert.ToString(_marker.y).Replace(",", ".") + "'";
                        query += " Where ID = " + _marker.id;
                        SqlCommand command = new SqlCommand(query, connection);
                        command.ExecuteNonQuery();
                        _marker.type = "old";
                        _markers[i] = _marker;
                    }
                }
                connection.Close();
            }
        }

        void insertDB()
        {
            string connectionString = @"Data Source=NIKOLAY;Initial Catalog=MarkersDB;Integrated Security=True";
            string query = "";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                for (int i =0; i < _markers.Count(); i++)
                {
                    if (_markers[i].type == "new")
                    {
                        Markers _marker = _markers[i];
                        query = "INSERT INTO [dbo].[Markers] ([Coordinates],[Title]) VALUES (";
                        query += "'" + Convert.ToString(_marker.x).Replace(",", ".") + "," + Convert.ToString(_marker.y).Replace(",", ".");
                        query += "', '" + _marker.title + "') select SCOPE_IDENTITY()";
                        SqlCommand command = new SqlCommand(query, connection);
                        _marker.id = Convert.ToInt32(command.ExecuteScalar());
                        _marker.type = "old";
                        _markers[i] = _marker;
                    }
                }
                connection.Close();
            }
        }

        void updateCoord(string title, double newX, double newY)
        {
            int id_marker = -1;
            for (int i = 0; i < _markers.Count(); i++)
            {
                if (_markers[i].title == title.Substring(1))
                {
                    id_marker = i;
                    break;
                }
            }
            if (id_marker != -1)
            {
                Markers _marker = _markers[id_marker];
                _marker.x = newX;
                _marker.y = newY;
                _marker.type = "update";
                _markers[id_marker] = _marker;
            }
        }

        private void _gMapControl_MouseDown(object sender, MouseEventArgs e)
        {   
            _selectedMarker = _gMapControl.Overlays.SelectMany(o => o.Markers).FirstOrDefault(m => m.IsMouseOver == true);
        }

        private void _gMapControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (_selectedMarker is null)
                return;

            var latlng = _gMapControl.FromLocalToLatLng(e.X, e.Y); 
            _selectedMarker.Position = latlng;
            updateCoord(_selectedMarker.ToolTipText, latlng.Lat, latlng.Lng);
            updateDB();
            _selectedMarker = null;
        }

        private void _gMapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedMarker is null)
                return;

            var latlng = _gMapControl.FromLocalToLatLng(e.X, e.Y);
            _selectedMarker.Position = latlng;
            
        }

        private void Coord_X_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if ((e.KeyChar <= 47 || e.KeyChar >= 58) && number != 8 && number != 44 && number != 45 && number != 46) //цифры, клавиша BackSpace, точка и запятая а ASCII
            {
                e.Handled = true;
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if ((e.KeyChar <= 47 || e.KeyChar >= 58) && number != 8 && number != 44 && number != 45 && number != 46) //цифры, клавиша BackSpace, точка и запятая а ASCII
            {
                e.Handled = true;
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if ((e.KeyChar <= 47 || e.KeyChar >= 58) && (e.KeyChar <= 64 || e.KeyChar >= 91) && (e.KeyChar <= 96 || e.KeyChar >= 123) && number != 8 && number != 40 && number != 41 && number != 45 && number != 95) 
            {
                e.Handled = true;
            }
        }
    }
}
