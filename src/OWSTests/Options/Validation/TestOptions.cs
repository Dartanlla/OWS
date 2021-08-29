using System.ComponentModel.DataAnnotations;

namespace OWSTests.Options.Validation
{
    public class TestOptions
    {
        public const string SectionName = "TestOptions";
    }

    public class TestOptionsRequired : TestOptions
    {
        [Required(ErrorMessage = "Field Is Required")]
        public string Field { get; set; }
    }

    public class TestOptionsType : TestOptions
    {
        [DataType(DataType.Text)]
        public string Field { get; set; }
    }

    public class TestOptionsRequiredFailed : TestOptions
    {
        [Required(ErrorMessage = "Field Is Required")]
        public string FieldRequired { get; set; }
    }

    public class TestOptionsTypeFailed : TestOptions
    {
        [Required(ErrorMessage = "Field Is Required")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Field is not numeric")]
        public string Field { get; set; }
    }

    public class TestOptionsMutipleErrors : TestOptions
    {
        [Required(ErrorMessage = "Field1 Is Required")]
        public string Field1 { get; set; }

        [Required(ErrorMessage = "Field2 Is Required")]
        public string Field2 { get; set; }
    }
}
