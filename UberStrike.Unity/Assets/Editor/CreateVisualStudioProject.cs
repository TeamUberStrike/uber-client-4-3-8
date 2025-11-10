using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CreateVisualStudioProject
{
    private enum WarningCodes
    {
        HidesInheritedMember = 0114,            //Really bad boy - keep this one always as error
        FieldNeverUsed = 0169,                  //Optional - good for cleanup phase
        FieldAssignedButNeverUsed = 0414,       //Optional - good for cleanup phase
        VariableAssignedButNeverUsed = 0219,    //Optional - good for cleanup phase
        FieldNeverAssignedButUsed = 0649,       //Unity Inspector field - keep this as ignored warning
        SelfAssignementOfFields = 1717,         //Really bad boy - keep this one always as error
    }

    private static WarningCodes[] WarningAsErrors = 
    {
        WarningCodes.HidesInheritedMember,
        //WarningCodes.FieldNeverUsed,
        WarningCodes.VariableAssignedButNeverUsed,
        WarningCodes.FieldAssignedButNeverUsed,
        WarningCodes.SelfAssignementOfFields,
     };

    private static string[] Extentions =
    {
        "xml", "shader", "tt"
    };

    private static WarningCodes[] WarningsIgnored = 
    {
        WarningCodes.FieldNeverAssignedButUsed,
    };

    [MenuItem("Assets/Create Visual StudioSolution")]
    public static void UpdateProject()
    {
        UpdateProject(true);
    }

    public static void UpdateProject(bool forceUpdate, string path = "Assets")
    {
        string vs_root = Directory.GetCurrentDirectory();
        int projectHashCode = 0;

        StreamReader reader = null;
        try
        {
            reader = new StreamReader(System.IO.Path.Combine(vs_root, ProjectName + ".csproj"));
            projectHashCode = reader.ReadToEnd().GetHashCode();
        }
        catch { }
        finally
        {
            if (reader != null)
                reader.Close();
        }

        //write solution file out to disk.
        StreamWriter solutionStream = new StreamWriter(System.IO.Path.Combine(vs_root, ProjectName + ".sln"));
        solutionStream.Write(GetSolutionText());
        solutionStream.Close();

        //write first part of our project file.
        StringBuilder projectStream = new StringBuilder();
        projectStream.AppendLine(GetProjectFileHead());

        //add a line for each .cs file found.
        DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(Directory.GetCurrentDirectory(), path));

        FileInfo[] fis = di.GetFiles("*.cs", SearchOption.AllDirectories);
        foreach (FileInfo fi in fis)
        {
            string relative = fi.FullName.Substring(di.FullName.Length + 1);
            relative = relative.Replace("/", "\\");
            if (File.Exists(fi.FullName.Replace(".cs", ".tt")))
            {
                projectStream.AppendLine("     <Compile Include=\"" + path + "\\" + relative + "\" >");
                projectStream.AppendLine("       <AutoGen>True</AutoGen>");
                projectStream.AppendLine("       <DesignTime>True</DesignTime>");
                projectStream.AppendLine("       <DependentUpon>" + fi.Name.Replace(".cs", ".tt") + "</DependentUpon>");
                projectStream.AppendLine("     </Compile>");
            }
            else
            {
                projectStream.AppendLine("     <Compile Include=\"" + path + "\\" + relative + "\" />");
            }
        }

        //add a line for each shader found.
        foreach (string ext in Extentions)
        {
            fis = di.GetFiles("*." + ext, SearchOption.AllDirectories);
            foreach (FileInfo fi in fis)
            {
                string relative = fi.FullName.Substring(di.FullName.Length + 1);
                relative = relative.Replace("/", "\\");
                if (ext == "tt")
                {
                    projectStream.AppendLine("     <None Include=\"" + path + "\\" + relative + "\" >");
                    projectStream.AppendLine("       <Generator>TextTemplatingFileGenerator</Generator>");
                    projectStream.AppendLine("       <LastGenOutput>" + fi.Name.Replace(".tt", ".cs") + "</LastGenOutput>");
                    projectStream.AppendLine("     </None>");
                }
                else
                {
                    projectStream.AppendLine("     <None Include=\"" + path + "\\" + relative + "\" />");
                }
            }
        }

        //and write the tail of our projectfile.
        projectStream.AppendLine(GetProjectFileTail());

        string content = projectStream.ToString();

        if (forceUpdate || projectHashCode != content.GetHashCode())
        {
            projectHashCode = content.GetHashCode();
            StreamWriter sw = new StreamWriter(System.IO.Path.Combine(vs_root, ProjectName + ".csproj"));
            sw.Write(content);
            sw.Close();

            Debug.Log("VisualStudio Project updated BASE");
        }
    }

    static string GetAssemblies()
    {
        DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Assets"));
        FileInfo[] dlls = di.GetFiles("*.dll", SearchOption.AllDirectories);
        StringBuilder builder = new StringBuilder();
        builder.AppendFormat("<Reference Include=~{0}~>\n", "UnityEngine");
        builder.AppendFormat("<HintPath>{0}</HintPath>\n", "../References/UnityEngine/UnityEngine.dll");
        builder.AppendLine("</Reference>");
        builder.AppendFormat("<Reference Include=~{0}~>\n", "UnityEditor");
        builder.AppendFormat("<HintPath>{0}</HintPath>\n", "../References/UnityEngine/UnityEditor.dll");
        builder.AppendLine("</Reference>");
        foreach (FileInfo fi in dlls)
        {
            builder.AppendFormat("<Reference Include=~{0}~>\n", fi.Name.Substring(0, fi.Name.Length - 4));
            builder.AppendFormat("<HintPath>Assets/{0}</HintPath>\n", fi.FullName.Substring(di.FullName.Length + 1));
            builder.AppendLine("</Reference>");
        }
        return builder.ToString();
    }

    static string ProjectName
    {
        get
        {
            return new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
        }
    }

    static string MyHash(string input)
    {
        byte[] bs = MD5.Create().ComputeHash(Encoding.Default.GetBytes(input));
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bs)
            sb.Append(b.ToString("x2"));
        string s = sb.ToString();

        s = s.Substring(0, 8) + "-" + s.Substring(8, 4) + "-" + s.Substring(12, 4) + "-" + s.Substring(16, 4) + "-" + s.Substring(20, 12);
        return s.ToUpper();
    }

    static string GetProjectGUID()
    {
        return MyHash(ProjectName + "salt");
    }

    static string GetAssemblyPath(Type t)
    {
        return Assembly.GetAssembly(t).Location.Replace("/", "\\");
    }

    static string GetSolutionGUID()
    {
        return MyHash(ProjectName);
    }

    static string GetSolutionText()
    {
        string t = @"Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
Project(~{" + GetSolutionGUID() + @"}~) = ~" + ProjectName + @"~, ~" + ProjectName + @".csproj~, ~{" + GetProjectGUID() + @"}~
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{" + GetProjectGUID() + @"}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{" + GetProjectGUID() + @"}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{" + GetProjectGUID() + @"}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{" + GetProjectGUID() + @"}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";
        return t.Replace("~", "\"");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    static string GetProjectFileHead()
    {
        StringBuilder ignore = new StringBuilder();
        foreach (int i in WarningsIgnored)
            ignore.Append(i).Append(", ");

        StringBuilder errors = new StringBuilder();
        foreach (int i in WarningAsErrors)
            errors.Append(i).Append(", ");

        string t = @"<?xml version=~1.0~ encoding=~utf-8~?>
<Project ToolsVersion=~4.0~ DefaultTargets=~Build~ xmlns=~http://schemas.microsoft.com/developer/msbuild/2003~>
  <PropertyGroup>
    <Configuration Condition=~ '$(Configuration)' == '' ~>Editor</Configuration>
    <Platform Condition=~ '$(Platform)' == '' ~>AnyCPU</Platform>
    <ProductVersion>9.0.21022</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{" + GetProjectGUID() + @"}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>" + ProjectName + @"</RootNamespace>
    <AssemblyName>" + ProjectName + @"</AssemblyName>
    <TargetFrameworkVersion>v3.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <FileUpgradeFlags>
    </FileUpgradeFlags>
    <OldToolsVersion>3.5</OldToolsVersion>
    <UpgradeBackupLocation />
  </PropertyGroup>
  <PropertyGroup Condition=~ '$(Configuration)|$(Platform)' == 'Editor|AnyCPU' ~>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\bin\Debug\</OutputPath>
	<DefineConstants>DEBUG;TRACE;UNITY_EDITOR;ENABLE_PROFILER;ENABLE_GENERICS;ENABLE_DUCK_TYPING;ENABLE_TERRAIN;ENABLE_MOVIES;ENABLE_NETWORK;ENABLE_CLOTH;ENABLE_WWW</DefineConstants>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
	<NoWarn>" + ignore.ToString() + @"</NoWarn>
    <CodeAnalysisRuleSet>AllRules.ruleset</CodeAnalysisRuleSet>
    <WarningsAsErrors>" + errors.ToString() + "</WarningsAsErrors>" +
  @"</PropertyGroup>
  <PropertyGroup Condition=~ '$(Configuration)|$(Platform)' == 'Android|AnyCPU' ~>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\bin\Debug\</OutputPath>
	<DefineConstants>DEBUG;TRACE;UNITY_ANDROID;ENABLE_PROFILER;ENABLE_GENERICS;ENABLE_DUCK_TYPING;ENABLE_TERRAIN;ENABLE_MOVIES;ENABLE_NETWORK;ENABLE_CLOTH;ENABLE_WWW</DefineConstants>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
	<NoWarn>" + ignore.ToString() + @"</NoWarn>
    <CodeAnalysisRuleSet>AllRules.ruleset</CodeAnalysisRuleSet>
    <WarningsAsErrors>" + errors.ToString() + "</WarningsAsErrors>" +
  @"</PropertyGroup>
  <PropertyGroup Condition=~ '$(Configuration)|$(Platform)' == 'iOS|AnyCPU' ~>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\bin\Debug\</OutputPath>
	<DefineConstants>DEBUG;TRACE;UNITY_IPHONE;ENABLE_PROFILER;ENABLE_GENERICS;ENABLE_DUCK_TYPING;ENABLE_TERRAIN;ENABLE_MOVIES;ENABLE_NETWORK;ENABLE_CLOTH;ENABLE_WWW</DefineConstants>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
	<NoWarn>" + ignore.ToString() + @"</NoWarn>
    <CodeAnalysisRuleSet>AllRules.ruleset</CodeAnalysisRuleSet>
    <WarningsAsErrors>" + errors.ToString() + "</WarningsAsErrors>" +
  @"</PropertyGroup>
  <PropertyGroup Condition=~ '$(Configuration)|$(Platform)' == 'MacOSX|AnyCPU' ~>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\bin\Debug\</OutputPath>
	<DefineConstants>DEBUG;TRACE;UNITY_STANDALONE_OSX;ENABLE_PROFILER;ENABLE_GENERICS;ENABLE_DUCK_TYPING;ENABLE_TERRAIN;ENABLE_MOVIES;ENABLE_NETWORK;ENABLE_CLOTH;ENABLE_WWW</DefineConstants>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
	<NoWarn>" + ignore.ToString() + @"</NoWarn>
    <CodeAnalysisRuleSet>AllRules.ruleset</CodeAnalysisRuleSet>
    <WarningsAsErrors>" + errors.ToString() + "</WarningsAsErrors>" +
  @"</PropertyGroup>
  <PropertyGroup Condition=~ '$(Configuration)|$(Platform)' == 'WebPlayer|AnyCPU' ~>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\bin\Debug\</OutputPath>
	<DefineConstants>DEBUG;TRACE;ENABLE_PROFILER;UNITY_WEBPLAYER;ENABLE_GENERICS;ENABLE_DUCK_TYPING;ENABLE_TERRAIN;ENABLE_MOVIES;ENABLE_NETWORK;ENABLE_CLOTH;ENABLE_WWW</DefineConstants>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
	<NoWarn>" + ignore.ToString() + @"</NoWarn>
    <CodeAnalysisRuleSet>AllRules.ruleset</CodeAnalysisRuleSet>
    <WarningsAsErrors>" + errors.ToString() + "</WarningsAsErrors>" +
  @"</PropertyGroup>
  <ItemGroup>
    <Reference Include=~System~ />
    <Reference Include=~System.Core~ />
    <Reference Include=~System.Xml~ />
" + GetAssemblies() +
  @"</ItemGroup>
  <ItemGroup>
";
        return t.Replace("~", "\"");
    }

    static string GetProjectFileTail()
    {
        string t = @"  </ItemGroup>
  <Import Project=~$(MSBuildToolsPath)\Microsoft.CSharp.targets~ />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=~BeforeBuild~>
  </Target>
  <Target Name=~AfterBuild~>
  </Target>
  -->
</Project>";
        return t.Replace("~", "\"");
    }
}