﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace webservice {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class templates {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal templates() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("webservice.templates", typeof(templates).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to       &lt;Row&gt;
        ///        &lt;Cell ss:MergeAcross=&quot;4&quot; ss:StyleID=&quot;table_footer&quot;/&gt;
        ///      &lt;/Row&gt;
        ///      &lt;Row /&gt;
        ///      &lt;Row&gt;
        ///        &lt;Cell ss:MergeAcross=&quot;4&quot; ss:StyleID=&quot;sign&quot;&gt;
        ///          &lt;Data ss:Type=&quot;String&quot;&gt;М.П.___________________      ____________________________________________         Дата________________&lt;/Data&gt;
        ///        &lt;/Cell&gt;
        ///      &lt;/Row&gt;
        ///      &lt;Row&gt;
        ///        &lt;Cell ss:MergeAcross=&quot;4&quot; ss:StyleID=&quot;sign&quot;&gt;
        ///          &lt;Data ss:Type=&quot;String&quot;&gt;                     (подпись)                                 (долж [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string export_rao_footer {
            get {
                return ResourceManager.GetString("export_rao_footer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot;?&gt;
        ///&lt;?mso-application progid=&quot;Excel.Sheet&quot;?&gt;
        ///&lt;Workbook xmlns=&quot;urn:schemas-microsoft-com:office:spreadsheet&quot;
        /// xmlns:o=&quot;urn:schemas-microsoft-com:office:office&quot;
        /// xmlns:x=&quot;urn:schemas-microsoft-com:office:excel&quot;
        /// xmlns:ss=&quot;urn:schemas-microsoft-com:office:spreadsheet&quot;
        /// xmlns:html=&quot;http://www.w3.org/TR/REC-html40&quot;&gt;
        ///  &lt;DocumentProperties xmlns=&quot;urn:schemas-microsoft-com:office:office&quot;&gt;
        ///    &lt;Author&gt;ЗАО &amp;quot;Мьюзик Один&amp;quot;&lt;/Author&gt;
        ///    &lt;Version&gt;1.0&lt;/Version&gt;
        ///  &lt;/DocumentProperties&gt;        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string export_rao_header {
            get {
                return ResourceManager.GetString("export_rao_header", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to       &lt;Row&gt;
        ///        &lt;Cell ss:StyleID=&quot;cell_left&quot;&gt;
        ///          &lt;Data ss:Type=&quot;String&quot;&gt;{%ARTIST_NAME%}&lt;/Data&gt;
        ///        &lt;/Cell&gt;
        ///        &lt;Cell ss:StyleID=&quot;cell_left&quot;&gt;
        ///          &lt;Data ss:Type=&quot;String&quot;&gt;{%SONG_NAME%}&lt;/Data&gt;
        ///        &lt;/Cell&gt;
        ///        &lt;Cell ss:StyleID=&quot;cell_middle&quot;&gt;
        ///          &lt;Data ss:Type=&quot;String&quot;&gt;{%SECONDS%}&lt;/Data&gt;
        ///        &lt;/Cell&gt;
        ///				&lt;Cell ss:StyleID=&quot;cell_right&quot;&gt;
        ///					&lt;Data ss:Type=&quot;Number&quot;&gt;{%RELISES_QTY%}&lt;/Data&gt;
        ///				&lt;/Cell&gt;
        ///				&lt;Cell ss:StyleID=&quot;cell_middle&quot;&gt;
        ///					&lt;Data ss:Type=&quot;String [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string export_rao_row {
            get {
                return ResourceManager.GetString("export_rao_row", resourceCulture);
            }
        }
    }
}
