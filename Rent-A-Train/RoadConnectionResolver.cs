using System;
using System.Collections.Generic;

namespace Rent_A_Train
{
    // Outside markers may store their lanes on an owned network node rather than
    // on the marker itself. Walk both relationships; never filter on bus-only roads.
    public static class RoadConnectionResolver
    {
        public static T FindLaneSource<T>(T start, Func<T,bool> hasLanes, Func<T,T> owner, Func<T,T> attached)
        {
            var pending=new List<T>{start};
            var visited=new HashSet<T>();
            var comparer=EqualityComparer<T>.Default;
            for(int i=0;i<pending.Count && i<64;i++)
            {
                T current=pending[i];
                if(comparer.Equals(current,default(T)) || !visited.Add(current))continue;
                if(hasLanes(current))return current;
                T parent=owner(current), attachment=attached(current);
                if(!comparer.Equals(parent,default(T)))pending.Add(parent);
                if(!comparer.Equals(attachment,default(T)))pending.Add(attachment);
            }
            return default(T);
        }
    }
}
