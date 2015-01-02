

namespace LibMeteoPL
{
    class LibMeteoPLTranslator
    {
        static void Main(string[] args)
        {
            new LibMeteoPLTranslatorToJava("../../../LibMeteoPLDotNet/ModelUM.cs", "../../../../JavaDerivative/LibMeteoPLJavaGenerated/").translate();


        }


    }
}
