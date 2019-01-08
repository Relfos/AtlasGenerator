using System;
using System.Collections;
using System.Collections.Generic;

namespace LunarLabs.Utils
{
    public class RectanglePacker<T>
    {
        protected class PackerRect
        {
            public T key;
            public int width;
            public int height;
            public bool done;
            public PackerNode node;
        }

        protected class PackerNode
        {
            public PackerNode[] child; // 0..1
            public int y;
            public int x;
            public int width;
            public int height;
            public PackerRect rect;

            public PackerNode(int x, int y, int width, int height)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
                this.rect = null;
                this.child = new PackerNode[2];
                this.child[0] = null;
                this.child[1] = null;
            }

            public PackerNode Insert(PackerRect rect)
            {
                PackerNode result;

                if (child[0] != null && child[1] != null)
                {
                    result = child[0].Insert(rect);
                    if (result != null)
                        return result;

                    result = child[1].Insert(rect);
                    return result;
                }


                //if there's already a lightmap here, return
                if (this.rect != null)
                {
                    return null;
                }

                // if we're too small, return
                if (rect.width > this.width || rect.height > this.height)
                {
                    return null;
                }

                // if we're just right, accept
                if (rect.width == this.width && rect.height == this.height)
                {
                    rect.done = true;
                    this.rect = rect;
                    return this;
                }

                // otherwise, gotta split this node and create some kids

                //decide which way to split
                int dw = this.width - rect.width;
                int dh = this.height - rect.height;

                if (dw > dh)
                {
                    this.child[0] = new PackerNode(this.x, this.y, rect.width, this.height);
                    this.child[1] = new PackerNode(this.x + rect.width, this.y, this.width - (rect.width), this.height);
                }
                else
                {
                    this.child[0] = new PackerNode(this.x, this.y, this.width, rect.height);
                    this.child[1] = new PackerNode(this.x, this.y + rect.height, this.width, this.height - (rect.height));
                }

                //insert into secondChannel child we created
                return child[0].Insert(rect);
            }
        }

        protected PackerNode _root;
		protected List<PackerRect> _rectList;
      
	    public RectanglePacker()
        {
            ClearRects();
        }

        public void AddRect(int width, int height, T key)
        {
	        if (width<=0 || height<=0) 
	        {
		        throw new Exception();
            }
	
	        PackerRect rect = new PackerRect();
            _rectList.Add(rect);

	        rect.width = width;
	        rect.height = height;
	        rect.key = key;
	        rect.node = null;
	        rect.done = false;
        }

        public bool GetRect(T key, out int x, out int y)
        {
            int n = -1;
            for (int i=0; i<_rectList.Count; i++)
            {
                if (_rectList[i].key.Equals(key)) 
                {
                    n = i;
                    break;
                }
            }

            if (n<0 || _rectList[n].node == null || !_rectList[n].done) 
            {
                x = 0;
                y = 0;
                return false;
            }
    
	        x = _rectList[n].node.x;
	        y = _rectList[n].node.y;
            return true;
        }
      
        public void DeleteRect(T key)
        {
            int n = -1;
            for (int i=0; i<_rectList.Count; i++)
            {
                if (_rectList[i].key.Equals(key)) 
                {
                    n = i;
                    break;
                }
            }

            if (n>=0) 
            {
                _rectList.RemoveAt(n);
            }

        }

        public void ClearRects()
        {
            _rectList = new List<PackerRect>();
            _root = null;               
        }

        /// <summary>
        ///  Tries packing all added rects, and returns number of rects that could not be packed.
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <returns></returns>
        public int Pack(int minX, int minY, int maxX, int maxY)
        {
            int count = 0;

            for (int i = 0; i < _rectList.Count; i++)
            {
                if (!_rectList[i].done)
                {
                    count++;
                }
            }

            int index = 0;

            _root = null;

	        while (count>0) 
	        {
                int max = 0;
		        for (int i=0; i<_rectList.Count; i++)
		        {
			        if (_rectList[i].done) 
                    {
                        continue;
                    }
				        

			        int k = _rectList[i].width * _rectList[i].height;
			        if (k > max) 
			        {
        				max = k;
				        index = i;
                    }
                }
		    

		        if (_root==null) 
                {
                    _root = new PackerNode(minX, minY, maxX, maxY);
                }
      

		        _rectList[index].node = _root.Insert(_rectList[index]);
                if (_rectList[index].node == null) 
                {
                    return count;
                }

		        count--;
            }

	        return count;
        }
    	 
	}	
}  
