using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0001

namespace local_rag_sk.Helpers
{

    internal static class MemoryHelper
    {
        public static async void PopulateInterestingFacts(SemanticTextMemory memory, string collectionName)
        {
            var facts = OrgFact.GetFacts();
            foreach (OrgFact fact in facts)
            {
                await memory.SaveInformationAsync(collection: collectionName, 
                    id: fact.Id, 
                    text: fact.Text);
            }
        }
    }

    public class OrgFact
    {
        public string Text { get; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Description { get; }
        public string AdditionalMetadata { get; }

        public OrgFact(string text, string description, string additionalMetadata)
        {
            Text = text;
            Description = description;
            AdditionalMetadata = additionalMetadata;
        }

        public static IEnumerable<OrgFact> GetFacts()
        {
            var facts = new OrgFact[]
                {
                    new("Our headquarters is located in Sydney, Australia.", "Headquarters", "City: Sydney"),
                    new("We have been in business for 25 years.", "Years in Operation", "Years: 25"),
                    new("Our corporate sponsor is the Melbourne Football Club.", "Corporate Sponsorship", "Team: Melbourne Football Club"),
                    new("We have 2 major departments.", "Departments", "Number: 2"),
                    new("Our team includes developers among other professionals.", "Occupation", "Job Title: Developer"),
                    new("Our team enjoys outdoor activities such as bushwalking.", "Team Activities", "Activity: Bushwalking"),
                    new("We have a company pet policy that allows dogs.", "Company Pet Policy", "Type: Dog"),
                    new("We prefer catering options featuring Australian cuisine.", "Catering Preferences", "Cuisine: Australian"),
                    new("We have expanded our operations to 5 countries.", "International Presence", "Countries: 5"),
                    new("Our staff includes graduates from the University of Sydney.", "Education", "University: Sydney"),
                    new("Our team is multilingual, speaking 3 languages.", "Languages Spoken", "Number: 3"),
                    new("We have a strict allergen policy, including precautions for peanuts.", "Allergen Policy", "Allergen: Peanuts"),
                    new("We support athletic achievements, such as participating in marathons.", "Athletic Support", "Event: Marathon"),
                    new("We have a company-wide collection of Australian art.", "Company Initiatives", "Item: Australian Art"),
                    new("Our team enjoys the Australian spring season for company events.", "Seasonal Preferences", "Season: Spring"),
                    new("Our corporate book club's favorite book is 'The Book Thief'.", "Corporate Book Club", "Book: The Book Thief"),
                    new("We offer vegetarian, vegan, gluten free and halal options in our corporate diet policy.", "Dietary Policies", "Diet: Vegetarian"),
                    new("We actively support volunteering in local community projects.", "Community Engagement", "Place: Local Community Projects"),
                    new("We aim to expand our presence to every continent.", "Expansion Goals", "Goal: Every Continent"),
                    new("Many of our staff members hold advanced degrees, including in Computer Science.", "Advanced Education", "Degree: Master's in Computer Science")
                };
            return facts;
        }
    }
}
