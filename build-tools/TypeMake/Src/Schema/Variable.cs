using System;
using System.Collections.Generic;

namespace TypeMake
{
    public enum VariableValueTag
    {
        Boolean,
        Integer,
        String,
        Path,
        StringSet
    }
    public class VariableValue
    {
        public VariableValueTag _Tag;

        public bool Boolean;
        public int Integer;
        public String String;
        public PathString Path;
        public HashSet<String> StringSet;

        public static VariableValue CreateBoolean(bool Value) { return new VariableValue { _Tag = VariableValueTag.Boolean, Boolean = Value }; }
        public static VariableValue CreateInteger(int Value) { return new VariableValue { _Tag = VariableValueTag.Integer, Integer = Value }; }
        public static VariableValue CreateString(String Value) { return new VariableValue { _Tag = VariableValueTag.String, String = Value }; }
        public static VariableValue CreatePath(PathString Value) { return new VariableValue { _Tag = VariableValueTag.Path, Path = Value }; }
        public static VariableValue CreateStringSet(HashSet<String> Value) { return new VariableValue { _Tag = VariableValueTag.StringSet, StringSet = Value }; }

        public bool OnBoolean { get { return _Tag == VariableValueTag.Boolean; } }
        public bool OnInteger { get { return _Tag == VariableValueTag.Integer; } }
        public bool OnString { get { return _Tag == VariableValueTag.String; } }
        public bool OnPath { get { return _Tag == VariableValueTag.Path; } }
        public bool OnStringSet { get { return _Tag == VariableValueTag.StringSet; } }
    }
    public enum VariableSpecTag
    {
        NotApply,
        Fixed,
        Boolean,
        Integer,
        String,
        Selection,
        Path,
        MultiSelection
    }
    public class VariableSpec
    {
        public VariableSpecTag _Tag;

        public VariableValue NotApply;
        public VariableValue Fixed;
        public BooleanSpec Boolean;
        public IntegerSpec Integer;
        public StringSpec String;
        public StringSelectionSpec Selection;
        public PathStringSpec Path;
        public MultiSelectionSpec MultiSelection;

        public static VariableSpec CreateNotApply(VariableValue Value) { return new VariableSpec { _Tag = VariableSpecTag.NotApply, NotApply = Value }; }
        public static VariableSpec CreateFixed(VariableValue Value) { return new VariableSpec { _Tag = VariableSpecTag.Fixed, Fixed = Value }; }
        public static VariableSpec CreateBoolean(BooleanSpec Value) { return new VariableSpec { _Tag = VariableSpecTag.Boolean, Boolean = Value }; }
        public static VariableSpec CreateInteger(IntegerSpec Value) { return new VariableSpec { _Tag = VariableSpecTag.Integer, Integer = Value }; }
        public static VariableSpec CreateString(StringSpec Value) { return new VariableSpec { _Tag = VariableSpecTag.String, String = Value }; }
        public static VariableSpec CreateSelection(StringSelectionSpec Value) { return new VariableSpec { _Tag = VariableSpecTag.Selection, Selection = Value }; }
        public static VariableSpec CreatePath(PathStringSpec Value) { return new VariableSpec { _Tag = VariableSpecTag.Path, Path = Value }; }
        public static VariableSpec CreateMultiSelection(MultiSelectionSpec Value) { return new VariableSpec { _Tag = VariableSpecTag.MultiSelection, MultiSelection = Value }; }

        public bool OnNotApply { get { return _Tag == VariableSpecTag.NotApply; } }
        public bool OnFixed { get { return _Tag == VariableSpecTag.Fixed; } }
        public bool OnBoolean { get { return _Tag == VariableSpecTag.Boolean; } }
        public bool OnInteger { get { return _Tag == VariableSpecTag.Integer; } }
        public bool OnString { get { return _Tag == VariableSpecTag.String; } }
        public bool OnSelection { get { return _Tag == VariableSpecTag.Selection; } }
        public bool OnPath { get { return _Tag == VariableSpecTag.Path; } }
        public bool OnMultiSelection { get { return _Tag == VariableSpecTag.MultiSelection; } }
    }
    public class BooleanSpec
    {
        public bool DefaultValue;
        public String InputDisplay;
    }
    public class IntegerSpec
    {
        public int DefaultValue;
        public String InputDisplay;
        public Func<int, KeyValuePair<bool, String>> Validator;
    }
    public class StringSpec
    {
        public String DefaultValue;
        public String InputDisplay;
        public bool IsPassword;
        public Func<String, KeyValuePair<bool, String>> Validator;
    }
    public class StringSelectionSpec
    {
        public String DefaultValue;
        public String InputDisplay;
        public HashSet<String> Selections;
        public Func<String, KeyValuePair<bool, String>> Validator;
        public Func<String, String> PostMapper;
    }
    public class PathStringSpec
    {
        public PathString DefaultValue;
        public String InputDisplay;
        public bool IsDirectory;
        public Func<PathString, KeyValuePair<bool, String>> Validator;
    }
    public class MultiSelectionSpec
    {
        public HashSet<String> DefaultValues;
        public String InputDisplay;
        public HashSet<String> Selections;
        public Func<List<String>, KeyValuePair<bool, String>> Validator;
    }
    public class VariableItem
    {
        public String VariableName;
        public List<String> DependentVariableNames;
        public bool IsHidden;
        public Func<VariableSpec> GetVariableSpec;
        public Action<VariableValue> SetVariableValue;
    }
}
