using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Hoardr.DeltaJob
{
    public class DeltaCursorEntity : TableEntity
    {
        public long DropboxUserId { get { return long.Parse(PartitionKey); } }
        public string Cursor { get; set; }

        public DeltaCursorEntity()
        {
        }

        public DeltaCursorEntity(long dropboxUserId, string cursor)
        {
            PartitionKey = dropboxUserId.ToString();
            RowKey = dropboxUserId.ToString();
            Cursor = cursor;
        }
    }
}
