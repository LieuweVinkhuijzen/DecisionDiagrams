using System;
using System.IO;
using System.Security.Cryptography;

namespace DecisionDiagrams {
    public class GenerateCNF {
        public static void generateCNFs(int nVariables, int nClauses, int nFormulas) {
            String fileName;
            for (int i = 0; i < nFormulas; i++) {
                fileName = $"cnf-formula-{nVariables}-{nClauses}-{i}.cnf";
                Generate(nVariables, nClauses, fileName);
            }
        }

        public static void Generate(int nVariables, int nClauses, String fileName) {
            StreamWriter outputFile = new StreamWriter(fileName);

            Random random = new Random();
            int var1, var2, var3;
            for (int i = 0; i < nClauses; i++) {
                var1 = random.Next(nVariables);
                var2 = random.Next(nVariables);
                var3 = random.Next(nVariables);
                // Set negation bits
                if (random.Next(2) == 0)
                    var1 = -var1;
                if (random.Next(2) == 0)
                    var2 = -var2;
                if (random.Next(2) == 0)
                    var3 = -var3;
                outputFile.WriteLine($"{var1},{var2},{var3}");
            }
            outputFile.Flush();
            outputFile.Close();
        }
    }
}