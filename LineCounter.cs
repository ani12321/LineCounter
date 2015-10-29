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



    [PluginTitle(Constants.Title)]
    [PluginAuthor("ani_xhaja@hotmail.com")]
    [PluginVersion(1, 0, 0)]

    public class LineCounter : PluginBase, IEventUser
    {
        private List<string> _files = new List<string>();
        private Parakeet.Plugin.Project _project;
        private int _bLoc = 0, _cLoc = 0, _pLoc = 0, _tLoc = 0, _mLoc = 0;

        
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



        private bool Start()
        {
            if (_project == null) return false;

            //reset
            _files = new List<string>();
            _pLoc = _cLoc = _bLoc = _tLoc = _mLoc = 0;
            
            if (!Directory.Exists(_project.ProjectDirectory))
            {
                // Send an error
                Exception e = new Exception("Project could not be found.");
                Parakeet.Plugin.Events.Error.SendSuppressedError(this, new ErrorEventArgs(e));
                return false;
            }

            try
            {
                GetAssets();
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);

                Exception e = new Exception("Something went wrong while loading project.");
                Parakeet.Plugin.Events.Error.SendSuppressedError(this, new ErrorEventArgs(e));
                return false;
            }

            try
            {
                CountLines();
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);

                Exception e = new Exception("Something went wrong.");
                Parakeet.Plugin.Events.Error.SendSuppressedError(this, new ErrorEventArgs(e));
                return false;
            }

            Log.Add(_project.ProjectDirectory + " " + _pLoc + " " + _cLoc + " " + _bLoc + " " + _tLoc);

            return true;
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
                }

                
                Count(data);

            }

        }


        /// <summary>
        /// Count the lines in a piece of code
        /// - Empty lines in comment blocks count as comments
        /// </summary>
        /// <param name="data">GML script to count</param>
        private void Count(string data)
        {
            /*
                - Empty lines in comment blocks count as comments
                - mixed lines count as both code line and comment line
            */

            bool cBlock = false;

            var lines = data.Split('\n');
            _tLoc += lines.Length;

            foreach (var line in lines)
            {
                //avoid trim twice
                string trimmed = line.Trim();

                if (line.Contains("/*") && line.Contains("*/"))
                    _cLoc++;
                else if (line.Contains("/*"))
                {
                    cBlock = true;
                    _cLoc++;
                }
                else if (line.Contains("*/"))
                {
                    cBlock = false;
                    _cLoc++;
                }
                else if (cBlock)
                    _cLoc++;
                else if (trimmed.StartsWith("//"))
                    _cLoc++;
                else if (trimmed == "")
                    _bLoc++;
                else
                {
                    _pLoc++;
                    if (trimmed.Contains("//"))
                        _mLoc++;
                }

            }


        }


        [MenuButton("Projects/LineCounter", Summary = "The number of lines of code for the project.")]
        public void Show()
        {
            if (Start())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Lines of code: " + _pLoc + "\n");
                sb.Append("Lines of comments: " + _cLoc + " + " + _mLoc + " mixed with code \n");
                sb.Append("Empty lines: " + _bLoc + "\n");
                sb.Append("Total raw lines: " + _tLoc + "\n");


                ShowDialog(this, sb.ToString(), Constants.Title);
            }
            else ShowDialog(this, "Please open a project.", Constants.Title);
        }

    }
}
