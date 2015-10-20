using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Parakeet.Plugin;
using Parakeet.Plugin.Attributes;
using Parakeet.Plugin.Events;

namespace LineCounter
{



    [PluginTitle("LineCounter")]
    [PluginAuthor("ani_xhaja@hotmail.com")]
    [PluginVersion(1, 0, 0)]

    public class LineCounter : PluginBase, IEventUser
    {
        private List<string> _files = new List<string>();
        private int _lineNumber;
        private Parakeet.Plugin.Project _project;
        private int _bLoc = 0, _cLoc = 0, _pLoc = 0;

        
        public void Subscribe()
        {
            Parakeet.Plugin.Events.Project.ProjectLoaded +=Project_ProjectLoaded;
        }

        public void Unsubscribe()
        {
            Parakeet.Plugin.Events.Project.ProjectLoaded -= Project_ProjectLoaded;
        }

        private void Project_ProjectLoaded(Parakeet.Plugin.Project project)
        {
            if (project == null)
                // Do nothing
                return;
            _project = project;
            
        }



        private void Start()
        {
            if (_project == null) return;

            //reset
            _files = new List<string>();
            _lineNumber = _pLoc = _cLoc = _bLoc = 0;

            _project.ProjectDirectory = _project.ProjectDirectory;
            if (!Directory.Exists(_project.ProjectDirectory))
            {
                // Send an error
                Exception e = new Exception("Project could not be found.");
                Parakeet.Plugin.Events.Error.SendSuppressedError(this, new ErrorEventArgs(e));
                return;
            }


            GetAssets();
            CountLines();

        }

        /// <summary>
        /// Get files that contain code
        /// </summary>
        private void GetAssets()
        {
            var title = _project.ProjectTitle;
            string project = _project.ProjectDirectory + "\\" + title + ".project.gmx";

            XDocument doc = XDocument.Load(project);

            //add objects
            if (doc.Root.Element("objects") != null)
            foreach (XElement obj in doc.Root.Element("objects").Descendants("object"))
                _files.Add(_project.ProjectDirectory + "\\" + obj.Value + ".object.gmx");

            //add scripts
            if (doc.Root.Element("scripts") != null)
            foreach (XElement obj in doc.Root.Element("scripts").Descendants("script"))
                _files.Add(_project.ProjectDirectory + "\\" + obj.Value);

            //add rooms
            if (doc.Root.Element("rooms") != null)
            foreach (XElement obj in doc.Root.Element("rooms").Descendants("room"))
                _files.Add(_project.ProjectDirectory + "\\" + obj.Value + ".room.gmx");

            //add shaders
            if (doc.Root.Element("shaders") != null)
            foreach (XElement obj in doc.Root.Element("shaders").Descendants("shader"))
                _files.Add(_project.ProjectDirectory + "\\" + obj.Value);


        }


        /// <summary>
        /// Read all the code in the files
        /// </summary>
        private void CountLines()
        {

            foreach (var file in _files)
            {
                string data = "";
                XDocument doc;

                if (file.Contains(".object.gmx"))
                {

                    doc = XDocument.Load(file);


                    foreach (XElement evnt in doc.Root.Element("events").Elements("event"))
                        foreach (var action in evnt.Elements("action"))
                            if (action.Element("arguments") != null)
                                foreach (var argument in action.Element("arguments").Elements("argument"))
                                    if (argument.Element("string") != null)
                                    {
                                        if (argument.Element("string").Value != null)
                                            data += argument.Element("string").Value;

                                    }

                }
                else if (file.Contains(".gml"))
                    data = File.ReadAllText(file);
                else if (file.Contains(".room.gmx"))
                {
                    doc = XDocument.Load(file);
                    var code = doc.Root.Element("code");
                    if (code != null)
                        data = code.Value;

                }
                else if(file.Contains(".shader"))
                {
                    data = File.ReadAllText(file);
                    //data = data.Replace("\n//######################_==_YOYO_SHADER_MARKER_==_######################@~", "");
                }


                Log.Add(file);

                _lineNumber += Count(data);

            }

        }


        /// <summary>
        /// Count the lines in a piece of code
        /// http://gmc.yoyogames.com/index.php?showtopic=494032#post_id_3661185
        /// </summary>
        /// <param name="data">GML script to count</param>
        private int Count(string data)
        {
            bool cBlock = false;
            int bLoc = 0, cLoc = 0, pLoc = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '/' && data[i + 1] == '*')
                    cBlock = true;
                else if (data[i] == '*' && data[i + 1] == '/')
                    cBlock = false;
                else if (data[i] == '/' && data[i + 1] == '/' && !cBlock)
                {
                    cLoc++;
                    continue;
                }
                else if (data[i] == '\r' && i > 0)
                {
                    if (data[i - 1] == '\n')
                        bLoc++;
                    continue;
                }
                else if (data[i] != '\n')
                    continue;

                if (!cBlock) pLoc++;
                else cLoc++;

            }

            _pLoc += pLoc;
            _bLoc += bLoc;
            _cLoc += cLoc;

            return pLoc;

        }


        [MenuButton("Projects/LineCounter", Summary = "The number of lines of code for the project.")]
        public void Show()
        {
            Start();
            StringBuilder sb = new StringBuilder();
            sb.Append( "Total lines of code: " + _pLoc + "\n" );
            sb.Append( "Total lines of comments: " + _cLoc + "\n" );
            //sb.Append( "Total empty lines: " + _bLoc + "\n" );


            ShowDialog(this, sb.ToString(), "LineCounter");
        }

    }
}
