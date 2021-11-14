using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalVideoPlayer
{
    class Media
    {
        private LocalMovie[] movies;
        private LocalShow[] shows;

        public Media(int m, int s)
        {
            movies = new LocalMovie[m];
            shows = new LocalShow[s];
        }

        public LocalMovie[] Movies
        {
            get => movies;
            set => movies = value;
        }

        public LocalShow[] Shows
        {
            get => shows;
            set => shows = value;
        }

        internal bool Compare(Media prevMedia)
        {
            if (this.movies.Length != prevMedia.movies.Length) return false;
            if (this.shows.Length != prevMedia.shows.Length) return false;

            for (int i = 0; i < this.movies.Length; i++)
            {
                if (!this.movies[i].Compare(prevMedia.movies[i])) return false;
            }

            for (int i = 0; i < this.shows.Length; i++)
            {
                if (!this.shows[i].Compare(prevMedia.shows[i])) return false;
            }

            return true;
        }
    }

    class LocalMovie
    {
        private int id;
        private string name;
        private string path;
        private string poster;
        private string backdrop;
        private string overview;
        DateTime? date;

        public LocalMovie(string n, string p)
        {
            name = n;
            path = p;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }
        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Path
        {
            get => path;
            set => path = value;
        }
        public string Backdrop
        {
            get => backdrop;
            set => backdrop = value;
        }

        public string Poster
        {
            get => poster;
            set => poster = value;
        }

        public string Overview
        {
            get => overview;
            set => overview = value;
        }

        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        internal bool Compare(LocalMovie localMovie)
        {
            if (!this.name.Equals(localMovie.Name)) return false;
            if (!this.path.Equals(localMovie.Path)) return false;

            return true;
        }
    }

    class LocalShow
    {
        private int id;
        private string name;
        private string path;
        private string overview;
        private string backdrop;
        private string poster;
        private LocalSeason[] seasons;
        DateTime? date;

        public LocalShow(string n, string p)
        {
            name = n;
            path = p;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Backdrop
        {
            get => backdrop;
            set => backdrop = value;
        }

        public string Poster
        {
            get => poster;
            set => poster = value;
        }

        public string Overview
        {
            get => overview;
            set => overview = value;
        }

        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        public LocalSeason[] Seasons
        {
            get => seasons;
            set => seasons = value;
        }

        internal bool Compare(LocalShow localShow)
        {
            if (!this.name.Equals(localShow.name)) return false;
            if (!this.path.Equals(localShow.path)) return false;
            if (this.seasons.Length != localShow.seasons.Length) return false;
            for(int i = 0; i < this.seasons.Length; i++)
            {
                if (!this.seasons[i].Compare(localShow.seasons[i])) return false;
            }
            return true;
        }
    }

    class LocalSeason
    {
        private int id;
        private LocalEpisode[] episodes;
        private string poster;
        DateTime? date;

        public LocalSeason(int i)
        {
            id = i;
        }

        private int Id
        {
            get => id;
            set => id = value;
        }

        public string Poster
        {
            get => poster;
            set => poster = value;
        }

        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        public LocalEpisode[] Episodes
        {
            get => episodes;
            set => episodes = value;
        }

        internal bool Compare(LocalSeason localSeason)
        {
            if (this.episodes.Length != localSeason.episodes.Length) return false;

            for(int i = 0; i < this.episodes.Length; i++)
            {
                if (!this.episodes[i].Compare(localSeason.episodes[i])) return false;
            }
            return true;
        }
    }

    class LocalEpisode
    {
        private int id;
        private string name;
        private string backdrop;
        private string overview;
        DateTime? date;

        public LocalEpisode(int i, string n)
        {
            id = i;
            name = n;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }
        public string Name
        {
            get => name;
            set => name = value;
        }
        public string Backdrop
        {
            get => backdrop;
            set => backdrop = value;
        }

        public string Overview
        {
            get => overview;
            set => overview = value;
        }
        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        internal bool Compare(LocalEpisode otherEpisode)
        {
            if (!this.name.Equals(otherEpisode.name)) return false;
            if (!this.id.Equals(otherEpisode.id)) return false;

            return true;
        }
    }
}
