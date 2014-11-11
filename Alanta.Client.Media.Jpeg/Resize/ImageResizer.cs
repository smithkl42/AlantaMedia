// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System;
using Alanta.Client.Media.Jpeg.Filtering;

namespace Alanta.Client.Media.Jpeg.Resize
{
    public class ResizeNotNeededException : Exception { }
    public class ResizeProgressChangedEventArgs : EventArgs { public double Progress; }

    public class ImageResizer
    {

        private Image _input;

        public ImageResizer(Image input)
        {
            _input = input;
        }

        public static bool ResizeNeeded(Image image, int maxEdgeLength)
        {
            double scale = (image.Width > image.Height) ?
                (double)maxEdgeLength / image.Width :
                (double)maxEdgeLength / image.Height;

            return scale < 1.0; // true if we must downscale
        }

        public Image Resize(int maxEdgeLength, ResamplingFilters technique)
        {
            double scale;

            if (_input.Width > _input.Height)
                scale = (double)maxEdgeLength / _input.Width;
            else
                scale = (double)maxEdgeLength / _input.Height;

            if (scale >= 1.0)
                throw new ResizeNotNeededException();
        	return Resize(scale, technique);
        }

        public Image Resize(int maxWidth, int maxHeight, ResamplingFilters technique)
        {
            double wFrac = (double)maxWidth / _input.Width;
            double hFrac = (double)maxHeight / _input.Height;

        	// Make the image as large as possible, while 
            // fitting in the supplied box and
            // obeying the aspect ratio

            double scale = wFrac < hFrac ? wFrac : hFrac;

            if (scale >= 1.0)
                throw new ResizeNotNeededException();
        	return Resize(scale, technique);
        }

        public Image Resize(double scale, ResamplingFilters technique)
        {
            var height = (int)(scale * _input.Height);
            var width = (int)(scale * _input.Width);

            Filter resizeFilter;

            switch (technique)
            {
                case ResamplingFilters.NearestNeighbor:
                    resizeFilter = new NNResize();
                    break;
                case ResamplingFilters.LowpassAntiAlias:
                    resizeFilter = new LowpassResize();
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new Image(_input.ColorModel, resizeFilter.Apply(_input.Raster, width, height));
        }

    }
}
