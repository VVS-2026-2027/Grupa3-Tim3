namespace Cvjecara_Latica.Services
{
    /// <summary>
    /// Računa cijenu dostave narudžbe na osnovu iznosa i grada.
    /// Razvijeno TDD pristupom (Task 3).
    /// </summary>
    public class DostavaService
    {
        public const double PragBesplatneDostave = 100.0;
        public const double CijenaDostaveSarajevo = 5.0;
        public const double CijenaDostaveOstaliGradovi = 10.0;
        private const string GradSarajevo = "Sarajevo";

        public double IzracunajCijenuDostave(double iznosNarudzbe, string grad)
        {
            ProvjeriUlaz(iznosNarudzbe, grad);

            if (iznosNarudzbe >= PragBesplatneDostave)
                return 0.0;

            return JeSarajevo(grad) ? CijenaDostaveSarajevo : CijenaDostaveOstaliGradovi;
        }

        private static void ProvjeriUlaz(double iznosNarudzbe, string grad)
        {
            if (iznosNarudzbe <= 0)
                throw new ArgumentOutOfRangeException(nameof(iznosNarudzbe), "Iznos narudžbe mora biti veći od 0.");

            if (string.IsNullOrWhiteSpace(grad))
                throw new ArgumentException("Grad dostave mora biti unesen.", nameof(grad));
        }

        private static bool JeSarajevo(string grad) =>
            string.Equals(grad.Trim(), GradSarajevo, StringComparison.OrdinalIgnoreCase);
    }
}