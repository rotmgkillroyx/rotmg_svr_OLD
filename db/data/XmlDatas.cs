using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

public class XmlDatas
{
    const int XML_COUNT = 36;

    static XmlDatas()
    {
        TypeToId = new Dictionary<short, string>();
        IdToType = new Dictionary<string, short>();
        TypeToElement = new Dictionary<short, XElement>();
        TileDescs = new Dictionary<short, TileDesc>();
        ItemDescs = new Dictionary<short, Item>();
        ObjectDescs = new Dictionary<short, ObjectDesc>();
        PortalDescs = new Dictionary<short, PortalDesc>();

        Stream stream;
        for (int i = 0; i < XML_COUNT; i++)
        {
            stream = typeof(XmlDatas).Assembly.GetManifestResourceStream("db.data.dat" + i + ".xml");
            ProcessXml(stream);
        }
        stream = typeof(XmlDatas).Assembly.GetManifestResourceStream("db.data.addition.xml");
        ProcessXml(stream);
        stream.Position = 0;
        using (StreamReader rdr = new StreamReader(stream))
            AdditionXml = rdr.ReadToEnd();
    }

    static void ProcessXml(Stream stream)
    {
        XElement root = XElement.Load(stream);
        foreach (var elem in root.Elements("Ground"))
        {
            short type = (short)Utils.FromString(elem.Attribute("type").Value);
            string id = elem.Attribute("id").Value;

            TypeToId[type] = id;
            IdToType[id] = type;
            TypeToElement[type] = elem;

            TileDescs[type] = new TileDesc(elem);
        }
        foreach (var elem in root.Elements("Object"))
        {
            if (elem.Element("Class") == null) continue;
            string cls = elem.Element("Class").Value;
            short type = (short)Utils.FromString(elem.Attribute("type").Value);
            string id = elem.Attribute("id").Value;

            TypeToId[type] = id;
            IdToType[id] = type;
            TypeToElement[type] = elem;

            if (cls == "Equipment")
                ItemDescs[type] = new Item(elem);
            else if (cls == "Character" || cls == "GameObject" || cls == "Wall" ||
                cls == "ConnectedWall" || cls == "CaveWall")
                ObjectDescs[type] = new ObjectDesc(elem);
            else if (cls == "Portal")
            {
                try
                {
                    PortalDescs[type] = new PortalDesc(elem);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error for portal: " + type + " id: " + id); 
                        /*3392,1792,1795,1796,1805,1806,1810,1825 -- no location, assume nexus?* 
    *  Tomb Portal of Cowardice,  Dungeon Portal,  Portal of Cowardice,  Realm Portal,  Glowing Portal of Cowardice,  Glowing Realm Portal,  Nexus Portal,  Locked Wine Cellar Portal*/
                }
            }
        }
    }


    public static readonly Dictionary<short, string> TypeToId;
    public static readonly Dictionary<string, short> IdToType;
    public static readonly Dictionary<short, XElement> TypeToElement;
    public static readonly Dictionary<short, TileDesc> TileDescs;
    public static readonly Dictionary<short, Item> ItemDescs;
    public static readonly Dictionary<short, ObjectDesc> ObjectDescs;
    public static readonly Dictionary<short, PortalDesc> PortalDescs;


    public static readonly string AdditionXml;
}