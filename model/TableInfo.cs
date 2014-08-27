﻿using System.ComponentModel;
using System.Xml.Serialization;

namespace model {
    public class TableInfo : ITableInfo {
        private const string DefaultOwner = "dbo";

        private TableInfo() {
            Owner = DefaultOwner;
        }
        public TableInfo(string owner, string name) {
            Owner = owner;
            Name = name;
        }

        [XmlAttribute]
        [DefaultValue(DefaultOwner)]
        public string Owner { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
    }

    public interface ITableInfo {
        string Owner { get; }
        string Name { get; }
    }
}