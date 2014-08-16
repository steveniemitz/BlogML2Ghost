using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlogML2Ghost
{
    class Program
    {
        static XNamespace NS = "http://www.blogml.com/2006/09/BlogML";
        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: blogml2ghost <input.xml> <output.json>");
                return;
            }

            string input = args[0];
            string output = args[1];

            var data = ConvertData(input);
            var metaData = new
            {
                exported_on = GetJsDate(DateTime.UtcNow),
                version = "000"
            };
            var final = new
            {
                meta = metaData,
                data = data
            };
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(final, new Newtonsoft.Json.JsonSerializerSettings()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
            });

            File.WriteAllText(output, str);

            Console.WriteLine("BlogML file converted successfully!");
        }

        static object ConvertData(string input)
        {
            var root = XElement.Load(File.OpenRead(input));
            var categories = root.Element(NS + "categories")
                .Elements(NS + "category")
                .Select((x, i) => new { x, i })
                .ToDictionary(x => (Guid)x.x.Attribute("id"), v => new
                {
                    id = v.i,
                    name = v.x.Element(NS + "title").Value,
                    slug = GetSlugFromTitle(v.x.Element(NS + "title").Value),
                    description = ""
                });

            var postsRoot = root.Element(NS + "posts");
            var posts = postsRoot.Elements(NS + "post");
            var transformedPosts = new List<object>();
            var postToTag = new List<object>();
       
            int postId = 0;
            
            foreach (var e in posts)
            {
                postId++;
                transformedPosts.Add(new
                {
                    id = postId,
                    title = e.Element(NS + "title").Value,
                    slug = GetSlugFromTitle(e.Element(NS + "title").Value),
                    html = e.Element(NS + "content").Value,
                    markdown = e.Element(NS + "content").Value,
                    image = (object)null,
                    featured = 0,
                    page = 0,
                    language = "en_US",
                    status = "published",
                    meta_title = (object)null,
                    meta_description = (object)null,
                    author_id = 1,
                    created_at = GetJsDate((DateTime)e.Attribute("date-created")),
                    created_by = 1,
                    updated_at = GetJsDate((DateTime)e.Attribute("date-modified")),
                    updated_by = 1,
                    published_at = GetJsDate((DateTime)e.Attribute("date-created")),
                    published_by = 1
                });

                var tags = e.Element(NS + "categories");
                if (tags != null)
                {
                    var moreTags = tags
                        .Elements(NS + "category")
                        .Select(x => new { post_id = postId, tag_id = categories[(Guid)x.Attribute("ref")].id });

                    postToTag.AddRange(moreTags);
                }
            }

            return new
            {
                posts = transformedPosts,
                tags = categories.Values.ToList(),
                posts_tags = postToTag
            };
        }

        private static long GetJsDate(DateTime dateTime)
        {
            return (long)(dateTime - epoch).TotalMilliseconds;
        }

        static string GetSlugFromTitle(string url)
        {
            return Regex.Replace(Regex.Replace(url, "[^A-Za-z0-9-]+", "-"), "-{2,}", "-");
        }
    }
}
