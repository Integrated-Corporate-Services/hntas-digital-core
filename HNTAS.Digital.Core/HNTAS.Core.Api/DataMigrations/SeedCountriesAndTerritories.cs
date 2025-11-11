using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.DataMigrations
{
    public class SeedCountriesAndTerritories : IDataMigration
    {
        private readonly AWSDocDbSettings _awsDocDbSettings;
        private readonly IMongoCollection<CountryAndTerritory> _countryAndTerritoryCollection;
        private readonly ILogger<SeedCountriesAndTerritories> _logger;

        public SeedCountriesAndTerritories(
           IOptions<AWSDocDbSettings> awsDocDbSettings,
           ILogger<SeedCountriesAndTerritories> logger)
        {
            _awsDocDbSettings = awsDocDbSettings.Value;
            _logger = logger;

            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. " +
                    "Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable");
            }

            if (string.IsNullOrEmpty(awsDocDbSettings.Value.DatabaseName))
            {
                _logger.LogCritical("MongoDB DatabaseName is missing in settings. CounterService cannot initialize.");
                throw new InvalidOperationException("MongoDB DatabaseName is not configured. Please check appsettings.json or environment variables.");
            }
            if (string.IsNullOrEmpty(awsDocDbSettings.Value.CountersCollectionName))
            {
                _logger.LogCritical("MongoDB OrgCountersCollectionName is missing in settings. CounterService cannot initialize.");
                throw new InvalidOperationException("MongoDB OrgCountersCollectionName is not configured. Please check appsettings.json or environment variables.");
            }

            try
            {
                var mongoClient = new MongoClient(connectionString);
                var mongoDatabase = mongoClient.GetDatabase(awsDocDbSettings.Value.DatabaseName);
                _countryAndTerritoryCollection = mongoDatabase.GetCollection<CountryAndTerritory>(awsDocDbSettings.Value.CountriesAndTerritoriesCollectionName);

                _logger.LogInformation("SeedCountriesAndTerritories initialized successfully. Connected to database '{DatabaseName}', using collection '{CollectionName}'.",
                    awsDocDbSettings.Value.DatabaseName, _countryAndTerritoryCollection.CollectionNamespace.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect to Db for CounterService. Check connection string and MongoDB server status.");
                throw new InvalidOperationException("CounterService failed to connect to MongoDB.", ex);
            }
        }

        public async Task RunAsync()
        {
            var count = await _countryAndTerritoryCollection.CountDocumentsAsync(FilterDefinition<CountryAndTerritory>.Empty);
            if (count == 0)
            {
                _logger.LogInformation("Seeding CountriesAndTerritories collection...");

                var seedData = new List<CountryAndTerritory>
                {
                    new CountryAndTerritory { Name = "Abu Dhabi", FullValue = "territory:AE-AZ" },
                    new CountryAndTerritory { Name = "Afghanistan", FullValue = "country:AF" },
                    new CountryAndTerritory { Name = "Ajman", FullValue = "territory:AE-AJ" },
                    new CountryAndTerritory { Name = "Akrotiri", FullValue = "territory:XQZ" },
                    new CountryAndTerritory { Name = "Aland Islands", FullValue = "territory:AX" },
                    new CountryAndTerritory { Name = "Albania", FullValue = "country:AL" },
                    new CountryAndTerritory { Name = "Algeria", FullValue = "country:DZ" },
                    new CountryAndTerritory { Name = "American Samoa", FullValue = "territory:AS" },
                    new CountryAndTerritory { Name = "Andorra", FullValue = "country:AD" },
                    new CountryAndTerritory { Name = "Angola", FullValue = "country:AO" },
                    new CountryAndTerritory { Name = "Anguilla", FullValue = "territory:AI" },
                    new CountryAndTerritory { Name = "Antarctica", FullValue = "territory:AQ" },
                    new CountryAndTerritory { Name = "Antigua and Barbuda", FullValue = "country:AG" },
                    new CountryAndTerritory { Name = "Argentina", FullValue = "country:AR" },
                    new CountryAndTerritory { Name = "Armenia", FullValue = "country:AM" },
                    new CountryAndTerritory { Name = "Aruba", FullValue = "territory:AW" },
                    new CountryAndTerritory { Name = "Ascension", FullValue = "territory:SH-AC" },
                    new CountryAndTerritory { Name = "Australia", FullValue = "country:AU" },
                    new CountryAndTerritory { Name = "Austria", FullValue = "country:AT" },
                    new CountryAndTerritory { Name = "Azerbaijan", FullValue = "country:AZ" },
                    new CountryAndTerritory { Name = "Bahamas", FullValue = "country:BS" },
                    new CountryAndTerritory { Name = "Bahrain", FullValue = "country:BH" },
                    new CountryAndTerritory { Name = "Bangladesh", FullValue = "country:BD" },
                    new CountryAndTerritory { Name = "Barbados", FullValue = "country:BB" },
                    new CountryAndTerritory { Name = "Belarus", FullValue = "country:BY" },
                    new CountryAndTerritory { Name = "Belgium", FullValue = "country:BE" },
                    new CountryAndTerritory { Name = "Belize", FullValue = "country:BZ" },
                    new CountryAndTerritory { Name = "Benin", FullValue = "country:BJ" },
                    new CountryAndTerritory { Name = "Bermuda", FullValue = "territory:BM" },
                    new CountryAndTerritory { Name = "Bhutan", FullValue = "country:BT" },
                    new CountryAndTerritory { Name = "Bolivia", FullValue = "country:BO" },
                    new CountryAndTerritory { Name = "Bonaire, Sint Eustatius and Saba", FullValue = "territory:BQ" },
                    new CountryAndTerritory { Name = "Bosnia and Herzegovina", FullValue = "country:BA" },
                    new CountryAndTerritory { Name = "Botswana", FullValue = "country:BW" },
                    new CountryAndTerritory { Name = "Bouvet Island", FullValue = "territory:BV" },
                    new CountryAndTerritory { Name = "Brazil", FullValue = "country:BR" },
                    new CountryAndTerritory { Name = "British Antarctic Territory", FullValue = "territory:AQ-GB" },
                    new CountryAndTerritory { Name = "British Indian Ocean Territory", FullValue = "territory:IO" },
                    new CountryAndTerritory { Name = "British Virgin Islands", FullValue = "territory:VG" },
                    new CountryAndTerritory { Name = "Brunei Darussalam", FullValue = "country:BN" },
                    new CountryAndTerritory { Name = "Bulgaria", FullValue = "country:BG" },
                    new CountryAndTerritory { Name = "Burkina Faso", FullValue = "country:BF" },
                    new CountryAndTerritory { Name = "Burundi", FullValue = "country:BI" },
                    new CountryAndTerritory { Name = "Cambodia", FullValue = "country:KH" },
                    new CountryAndTerritory { Name = "Cameroon", FullValue = "country:CM" },
                    new CountryAndTerritory { Name = "Canada", FullValue = "country:CA" },
                    new CountryAndTerritory { Name = "Cape Verde", FullValue = "country:CV" },
                    new CountryAndTerritory { Name = "Cayman Islands", FullValue = "territory:KY" },
                    new CountryAndTerritory { Name = "Central African Republic", FullValue = "country:CF" },
                    new CountryAndTerritory { Name = "Chad", FullValue = "country:TD" },
                    new CountryAndTerritory { Name = "Chile", FullValue = "country:CL" },
                    new CountryAndTerritory { Name = "China", FullValue = "country:CN" },
                    new CountryAndTerritory { Name = "Christmas Island", FullValue = "territory:CX" },
                    new CountryAndTerritory { Name = "Cocos (Keeling) Islands", FullValue = "territory:CC" },
                    new CountryAndTerritory { Name = "Colombia", FullValue = "country:CO" },
                    new CountryAndTerritory { Name = "Comoros", FullValue = "country:KM" },
                    new CountryAndTerritory { Name = "Congo (Brazzaville)", FullValue = "country:CG" },
                    new CountryAndTerritory { Name = "Congo (Kinshasa)", FullValue = "country:CD" },
                    new CountryAndTerritory { Name = "Cook Islands", FullValue = "territory:CK" },
                    new CountryAndTerritory { Name = "Costa Rica", FullValue = "country:CR" },
                    new CountryAndTerritory { Name = "Cote d'Ivoire", FullValue = "country:CI" },
                    new CountryAndTerritory { Name = "Croatia", FullValue = "country:HR" },
                    new CountryAndTerritory { Name = "Cuba", FullValue = "country:CU" },
                    new CountryAndTerritory { Name = "Curacao", FullValue = "territory:CW" },
                    new CountryAndTerritory { Name = "Cyprus", FullValue = "country:CY" },
                    new CountryAndTerritory { Name = "Czech Republic", FullValue = "country:CZ" },
                    new CountryAndTerritory { Name = "Dhekelia", FullValue = "territory:XQY" },
                    new CountryAndTerritory { Name = "Denmark", FullValue = "country:DK" },
                    new CountryAndTerritory { Name = "Djibouti", FullValue = "country:DJ" },
                    new CountryAndTerritory { Name = "Dominica", FullValue = "country:DM" },
                    new CountryAndTerritory { Name = "Dominican Republic", FullValue = "country:DO" },
                    new CountryAndTerritory { Name = "Dubai", FullValue = "territory:AE-DU" },
                    new CountryAndTerritory { Name = "East Timor", FullValue = "country:TL" },
                    new CountryAndTerritory { Name = "Ecuador", FullValue = "country:EC" },
                    new CountryAndTerritory { Name = "Egypt", FullValue = "country:EG" },
                    new CountryAndTerritory { Name = "El Salvador", FullValue = "country:SV" },
                    new CountryAndTerritory { Name = "Equatorial Guinea", FullValue = "country:GQ" },
                    new CountryAndTerritory { Name = "Eritrea", FullValue = "country:ER" },
                    new CountryAndTerritory { Name = "Estonia", FullValue = "country:EE" },
                    new CountryAndTerritory { Name = "Eswatini", FullValue = "country:SZ" },
                    new CountryAndTerritory { Name = "Ethiopia", FullValue = "country:ET" },
                    new CountryAndTerritory { Name = "Falkland Islands", FullValue = "territory:FK" },
                    new CountryAndTerritory { Name = "Faroe Islands", FullValue = "territory:FO" },
                    new CountryAndTerritory { Name = "Fiji", FullValue = "country:FJ" },
                    new CountryAndTerritory { Name = "Finland", FullValue = "country:FI" },
                    new CountryAndTerritory { Name = "France", FullValue = "country:FR" },
                    new CountryAndTerritory { Name = "French Guiana", FullValue = "territory:GF" },
                    new CountryAndTerritory { Name = "French Polynesia", FullValue = "territory:PF" },
                    new CountryAndTerritory { Name = "French Southern Territories", FullValue = "territory:TF" },
                    new CountryAndTerritory { Name = "Fujairah", FullValue = "territory:AE-FU" },
                    new CountryAndTerritory { Name = "Gabon", FullValue = "country:GA" },
                    new CountryAndTerritory { Name = "Gambia", FullValue = "country:GM" },
                    new CountryAndTerritory { Name = "Georgia", FullValue = "country:GE" },
                    new CountryAndTerritory { Name = "Germany", FullValue = "country:DE" },
                    new CountryAndTerritory { Name = "Ghana", FullValue = "country:GH" },
                    new CountryAndTerritory { Name = "Gibraltar", FullValue = "territory:GI" },
                    new CountryAndTerritory { Name = "Greece", FullValue = "country:GR" },
                    new CountryAndTerritory { Name = "Greenland", FullValue = "territory:GL" },
                    new CountryAndTerritory { Name = "Grenada", FullValue = "country:GD" },
                    new CountryAndTerritory { Name = "Guadeloupe", FullValue = "territory:GP" },
                    new CountryAndTerritory { Name = "Guam", FullValue = "territory:GU" },
                    new CountryAndTerritory { Name = "Guatemala", FullValue = "country:GT" },
                    new CountryAndTerritory { Name = "Guernsey, Alderney, Sark", FullValue = "territory:GG" },
                    new CountryAndTerritory { Name = "Guinea", FullValue = "country:GN" },
                    new CountryAndTerritory { Name = "Guinea-Bissau", FullValue = "country:GW" },
                    new CountryAndTerritory { Name = "Guyana", FullValue = "country:GY" },
                    new CountryAndTerritory { Name = "Haiti", FullValue = "country:HT" },
                    new CountryAndTerritory { Name = "Heard Island and McDonald Islands", FullValue = "territory:HM" },
                    new CountryAndTerritory { Name = "Honduras", FullValue = "country:HN" },
                    new CountryAndTerritory { Name = "Hong Kong", FullValue = "territory:HK" },
                    new CountryAndTerritory { Name = "Hungary", FullValue = "country:HU" },
                    new CountryAndTerritory { Name = "Iceland", FullValue = "country:IS" },
                    new CountryAndTerritory { Name = "India", FullValue = "country:IN" },
                    new CountryAndTerritory { Name = "Indonesia", FullValue = "country:ID" },
                    new CountryAndTerritory { Name = "Iran", FullValue = "country:IR" },
                    new CountryAndTerritory { Name = "Iraq", FullValue = "country:IQ" },
                    new CountryAndTerritory { Name = "Ireland", FullValue = "country:IE" },
                    new CountryAndTerritory { Name = "Isle of Man", FullValue = "territory:IM" },
                    new CountryAndTerritory { Name = "Israel", FullValue = "country:IL" },
                    new CountryAndTerritory { Name = "Italy", FullValue = "country:IT" },
                    new CountryAndTerritory { Name = "Jamaica", FullValue = "country:JM" },
                    new CountryAndTerritory { Name = "Japan", FullValue = "country:JP" },
                    new CountryAndTerritory { Name = "Jersey", FullValue = "territory:JE" },
                    new CountryAndTerritory { Name = "Jordan", FullValue = "country:JO" },
                    new CountryAndTerritory { Name = "Kazakhstan", FullValue = "country:KZ" },
                    new CountryAndTerritory { Name = "Kenya", FullValue = "country:KE" },
                    new CountryAndTerritory { Name = "Kiribati", FullValue = "country:KI" },
                    new CountryAndTerritory { Name = "Kosovo", FullValue = "country:XK" },
                    new CountryAndTerritory { Name = "Kuwait", FullValue = "country:KW" },
                    new CountryAndTerritory { Name = "Kyrgyzstan", FullValue = "country:KG" },
                    new CountryAndTerritory { Name = "Laos", FullValue = "country:LA" },
                    new CountryAndTerritory { Name = "Latvia", FullValue = "country:LV" },
                    new CountryAndTerritory { Name = "Lebanon", FullValue = "country:LB" },
                    new CountryAndTerritory { Name = "Lesotho", FullValue = "country:LS" },
                    new CountryAndTerritory { Name = "Liberia", FullValue = "country:LR" },
                    new CountryAndTerritory { Name = "Libya", FullValue = "country:LY" },
                    new CountryAndTerritory { Name = "Liechtenstein", FullValue = "country:LI" },
                    new CountryAndTerritory { Name = "Lithuania", FullValue = "country:LT" },
                    new CountryAndTerritory { Name = "Luxembourg", FullValue = "country:LU" },
                    new CountryAndTerritory { Name = "Macao", FullValue = "territory:MO" },
                    new CountryAndTerritory { Name = "Madagascar", FullValue = "country:MG" },
                    new CountryAndTerritory { Name = "Malawi", FullValue = "country:MW" },
                    new CountryAndTerritory { Name = "Malaysia", FullValue = "country:MY" },
                    new CountryAndTerritory { Name = "Maldives", FullValue = "country:MV" },
                    new CountryAndTerritory { Name = "Mali", FullValue = "country:ML" },
                    new CountryAndTerritory { Name = "Malta", FullValue = "country:MT" },
                    new CountryAndTerritory { Name = "Marshall Islands", FullValue = "country:MH" },
                    new CountryAndTerritory { Name = "Martinique", FullValue = "territory:MQ" },
                    new CountryAndTerritory { Name = "Mauritania", FullValue = "country:MR" },
                    new CountryAndTerritory { Name = "Mauritius", FullValue = "country:MU" },
                    new CountryAndTerritory { Name = "Mayotte", FullValue = "territory:YT" },
                    new CountryAndTerritory { Name = "Mexico", FullValue = "country:MX" },
                    new CountryAndTerritory { Name = "Micronesia (Federated States of)", FullValue = "country:FM" },
                    new CountryAndTerritory { Name = "Moldova", FullValue = "country:MD" },
                    new CountryAndTerritory { Name = "Monaco", FullValue = "country:MC" },
                    new CountryAndTerritory { Name = "Mongolia", FullValue = "country:MN" },
                    new CountryAndTerritory { Name = "Montenegro", FullValue = "country:ME" },
                    new CountryAndTerritory { Name = "Montserrat", FullValue = "territory:MS" },
                    new CountryAndTerritory { Name = "Morocco", FullValue = "country:MA" },
                    new CountryAndTerritory { Name = "Mozambique", FullValue = "country:MZ" },
                    new CountryAndTerritory { Name = "Myanmar (Burma)", FullValue = "country:MM" },
                    new CountryAndTerritory { Name = "Namibia", FullValue = "country:NA" },
                    new CountryAndTerritory { Name = "Nauru", FullValue = "country:NR" },
                    new CountryAndTerritory { Name = "Nepal", FullValue = "country:NP" },
                    new CountryAndTerritory { Name = "Netherlands", FullValue = "country:NL" },
                    new CountryAndTerritory { Name = "New Caledonia", FullValue = "territory:NC" },
                    new CountryAndTerritory { Name = "New Zealand", FullValue = "country:NZ" },
                    new CountryAndTerritory { Name = "Nicaragua", FullValue = "country:NI" },
                    new CountryAndTerritory { Name = "Niger", FullValue = "country:NE" },
                    new CountryAndTerritory { Name = "Nigeria", FullValue = "country:NG" },
                    new CountryAndTerritory { Name = "Niue", FullValue = "territory:NU" },
                    new CountryAndTerritory { Name = "Norfolk Island", FullValue = "territory:NF" },
                    new CountryAndTerritory { Name = "North Korea", FullValue = "country:KP" },
                    new CountryAndTerritory { Name = "North Macedonia", FullValue = "country:MK" },
                    new CountryAndTerritory { Name = "Northern Mariana Islands", FullValue = "territory:MP" },
                    new CountryAndTerritory { Name = "Norway", FullValue = "country:NO" },
                    new CountryAndTerritory { Name = "Oman", FullValue = "country:OM" },
                    new CountryAndTerritory { Name = "Pakistan", FullValue = "country:PK" },
                    new CountryAndTerritory { Name = "Palau", FullValue = "country:PW" },
                    new CountryAndTerritory { Name = "Palestine, State of", FullValue = "country:PS" },
                    new CountryAndTerritory { Name = "Panama", FullValue = "country:PA" },
                    new CountryAndTerritory { Name = "Papua New Guinea", FullValue = "country:PG" },
                    new CountryAndTerritory { Name = "Paraguay", FullValue = "country:PY" },
                    new CountryAndTerritory { Name = "Peru", FullValue = "country:PE" },
                    new CountryAndTerritory { Name = "Philippines", FullValue = "country:PH" },
                    new CountryAndTerritory { Name = "Pitcairn, Henderson, Ducie and Oeno Islands", FullValue = "territory:PN" },
                    new CountryAndTerritory { Name = "Poland", FullValue = "country:PL" },
                    new CountryAndTerritory { Name = "Portugal", FullValue = "country:PT" },
                    new CountryAndTerritory { Name = "Puerto Rico", FullValue = "territory:PR" },
                    new CountryAndTerritory { Name = "Qatar", FullValue = "country:QA" },
                    new CountryAndTerritory { Name = "Ras al-Khaimah", FullValue = "territory:AE-RA" },
                    new CountryAndTerritory { Name = "Reunion", FullValue = "territory:RE" },
                    new CountryAndTerritory { Name = "Romania", FullValue = "country:RO" },
                    new CountryAndTerritory { Name = "Russia", FullValue = "country:RU" },
                    new CountryAndTerritory { Name = "Rwanda", FullValue = "country:RW" },
                    new CountryAndTerritory { Name = "Saba", FullValue = "territory:BQ-SB" },
                    new CountryAndTerritory { Name = "Saint Barthelemy", FullValue = "territory:BL" },
                    new CountryAndTerritory { Name = "Saint Helena, Ascension and Tristan da Cunha", FullValue = "territory:SH" },
                    new CountryAndTerritory { Name = "Saint Kitts and Nevis", FullValue = "country:KN" },
                    new CountryAndTerritory { Name = "Saint Lucia", FullValue = "country:LC" },
                    new CountryAndTerritory { Name = "Saint Martin (French part)", FullValue = "territory:MF" },
                    new CountryAndTerritory { Name = "Saint Pierre and Miquelon", FullValue = "territory:PM" },
                    new CountryAndTerritory { Name = "Saint Vincent and the Grenadines", FullValue = "country:VC" },
                    new CountryAndTerritory { Name = "Samoa", FullValue = "country:WS" },
                    new CountryAndTerritory { Name = "San Marino", FullValue = "country:SM" },
                    new CountryAndTerritory { Name = "Sao Tome and Principe", FullValue = "country:ST" },
                    new CountryAndTerritory { Name = "Saudi Arabia", FullValue = "country:SA" },
                    new CountryAndTerritory { Name = "Senegal", FullValue = "country:SN" },
                    new CountryAndTerritory { Name = "Serbia", FullValue = "country:RS" },
                    new CountryAndTerritory { Name = "Seychelles", FullValue = "country:SC" },
                    new CountryAndTerritory { Name = "Sharjah", FullValue = "territory:AE-SH" },
                    new CountryAndTerritory { Name = "Sierra Leone", FullValue = "country:SL" },
                    new CountryAndTerritory { Name = "Singapore", FullValue = "country:SG" },
                    new CountryAndTerritory { Name = "Sint Eustatius", FullValue = "territory:BQ-SE" },
                    new CountryAndTerritory { Name = "Sint Maarten (Dutch part)", FullValue = "territory:SX" },
                    new CountryAndTerritory { Name = "Slovakia", FullValue = "country:SK" },
                    new CountryAndTerritory { Name = "Slovenia", FullValue = "country:SI" },
                    new CountryAndTerritory { Name = "Solomon Islands", FullValue = "country:SB" },
                    new CountryAndTerritory { Name = "Somalia", FullValue = "country:SO" },
                    new CountryAndTerritory { Name = "South Africa", FullValue = "country:ZA" },
                    new CountryAndTerritory { Name = "South Georgia and the South Sandwich Islands", FullValue = "territory:GS" },
                    new CountryAndTerritory { Name = "South Korea", FullValue = "country:KR" },
                    new CountryAndTerritory { Name = "South Sudan", FullValue = "country:SS" },
                    new CountryAndTerritory { Name = "Spain", FullValue = "country:ES" },
                    new CountryAndTerritory { Name = "Sri Lanka", FullValue = "country:LK" },
                    new CountryAndTerritory { Name = "Sudan", FullValue = "country:SD" },
                    new CountryAndTerritory { Name = "Suriname", FullValue = "country:SR" },
                    new CountryAndTerritory { Name = "Svalbard and Jan Mayen", FullValue = "territory:SJ" },
                    new CountryAndTerritory { Name = "Sweden", FullValue = "country:SE" },
                    new CountryAndTerritory { Name = "Switzerland", FullValue = "country:CH" },
                    new CountryAndTerritory { Name = "Syria", FullValue = "country:SY" },
                    new CountryAndTerritory { Name = "Taiwan", FullValue = "country:TW" },
                    new CountryAndTerritory { Name = "Tajikistan", FullValue = "country:TJ" },
                    new CountryAndTerritory { Name = "Tanzania", FullValue = "country:TZ" },
                    new CountryAndTerritory { Name = "Thailand", FullValue = "country:TH" },
                    new CountryAndTerritory { Name = "Togo", FullValue = "country:TG" },
                    new CountryAndTerritory { Name = "Tokelau", FullValue = "territory:TK" },
                    new CountryAndTerritory { Name = "Tonga", FullValue = "country:TO" },
                    new CountryAndTerritory { Name = "Trinidad and Tobago", FullValue = "country:TT" },
                    new CountryAndTerritory { Name = "Tristan da Cunha", FullValue = "territory:SH-TA" },
                    new CountryAndTerritory { Name = "Tunisia", FullValue = "country:TN" },
                    new CountryAndTerritory { Name = "Turkey", FullValue = "country:TR" },
                    new CountryAndTerritory { Name = "Turkmenistan", FullValue = "country:TM" },
                    new CountryAndTerritory { Name = "Turks and Caicos Islands", FullValue = "territory:TC" },
                    new CountryAndTerritory { Name = "Tuvalu", FullValue = "country:TV" },
                    new CountryAndTerritory { Name = "Uganda", FullValue = "country:UG" },
                    new CountryAndTerritory { Name = "Umm al-Quwain", FullValue = "territory:AE-UQ" },
                    new CountryAndTerritory { Name = "Ukraine", FullValue = "country:UA" },
                    new CountryAndTerritory { Name = "United Arab Emirates", FullValue = "country:AE" },
                    new CountryAndTerritory { Name = "United Kingdom", FullValue = "country:GB" },
                    new CountryAndTerritory { Name = "United States", FullValue = "country:US" },
                    new CountryAndTerritory { Name = "Uruguay", FullValue = "country:UY" },
                    new CountryAndTerritory { Name = "Uzbekistan", FullValue = "country:UZ" },
                    new CountryAndTerritory { Name = "Vanuatu", FullValue = "country:VU" },
                    new CountryAndTerritory { Name = "Vatican City", FullValue = "country:VA" },
                    new CountryAndTerritory { Name = "Venezuela", FullValue = "country:VE" },
                    new CountryAndTerritory { Name = "Vietnam", FullValue = "country:VN" },
                    new CountryAndTerritory { Name = "Virgin Islands (US)", FullValue = "territory:VI" },
                    new CountryAndTerritory { Name = "Wallis and Futuna", FullValue = "territory:WF" },
                    new CountryAndTerritory { Name = "Western Sahara", FullValue = "territory:EH" },
                    new CountryAndTerritory { Name = "Yemen", FullValue = "country:YE" },
                    new CountryAndTerritory { Name = "Zambia", FullValue = "country:ZM" },
                    new CountryAndTerritory { Name = "Zimbabwe", FullValue = "country:ZW" }
                };

                await _countryAndTerritoryCollection.InsertManyAsync(seedData);

                _logger.LogInformation("Seeding completed successfully.");
            }
            else
            {
                _logger.LogInformation("CountriesAndTerritories collection already contains data. Skipping seeding.");
            }
        }
    }

}
