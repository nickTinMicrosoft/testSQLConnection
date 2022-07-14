using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testSQLConnection
{
    public class ToDoItem
    {
        public string Id { get; set; }
        public int? order { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public bool? completed { get; set; }

        public DateTime expirationdate { get; set; }

    }
}
