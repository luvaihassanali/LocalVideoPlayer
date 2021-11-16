using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalVideoPlayer
{
    class Media
    {
        private Movie[] movies;
        private TvShow[] tvShows;

        public Media(int m, int s)
        {
            movies = new Movie[m];
            tvShows = new TvShow[s];
        }

        public Movie[] Movies
        {
            get => movies;
            set => movies = value;
        }

        public TvShow[] TvShows
        {
            get => tvShows;
            set => tvShows = value;
        }

        internal bool Compare(Media prevMedia)
        {
            if (this.movies.Length != prevMedia.movies.Length) return false;
            if (this.tvShows.Length != prevMedia.tvShows.Length) return false;

            for (int i = 0; i < this.movies.Length; i++)
            {
                if (!this.movies[i].Compare(prevMedia.movies[i])) return false;
            }

            for (int i = 0; i < this.tvShows.Length; i++)
            {
                if (!this.tvShows[i].Compare(prevMedia.tvShows[i])) return false;
            }

            return true;
        }
        
        internal void Ingest(Media prevMedia)
        {
            for (int i = 0; i < prevMedia.Movies.Length; i++)
            {
                if (this.movies[i].Name.Equals(prevMedia.movies[i].Name))
                {
                    this.movies[i].Name = prevMedia.movies[i].Name;
                    this.movies[i].Overview = prevMedia.movies[i].Overview;
                    this.movies[i].Path = prevMedia.movies[i].Path;
                    this.movies[i].Poster = prevMedia.movies[i].Poster;
                    this.movies[i].Id = prevMedia.movies[i].Id;
                    this.movies[i].Date = prevMedia.movies[i].Date;
                    this.movies[i].Backdrop = prevMedia.movies[i].Backdrop;
                }
            }

            for (int i = 0; i < prevMedia.TvShows.Length; i++)
            {
                if (this.tvShows[i].Name.Equals(prevMedia.TvShows[i].Name))
                {
                    this.tvShows[i].Name = prevMedia.tvShows[i].Name;
                    this.tvShows[i].Id = prevMedia.tvShows[i].Id;
                    this.tvShows[i].Overview = prevMedia.tvShows[i].Overview;
                    this.tvShows[i].Poster = prevMedia.tvShows[i].Poster;
                    this.tvShows[i].Date = prevMedia.tvShows[i].Date;
                    this.tvShows[i].Backdrop = prevMedia.tvShows[i].Backdrop;

                    for (int j = 0; j < prevMedia.TvShows[i].Seasons.Length; j++)
                    {
                        this.tvShows[i].Seasons[j].Id = prevMedia.TvShows[j].Seasons[j].Id;
                        this.tvShows[i].Seasons[j].Poster = prevMedia.TvShows[j].Seasons[j].Poster;
                        this.tvShows[i].Seasons[j].Date = prevMedia.TvShows[j].Seasons[j].Date;

                        for (int k = 0; k < prevMedia.TvShows[i].Seasons[j].Episodes.Length; k++)
                        {
                            if (this.tvShows[i].Seasons[j].Episodes[k].Name.Equals(prevMedia.TvShows[i].Seasons[j].Episodes[k].Name))
                            {
                                this.tvShows[i].Seasons[j].Episodes[k].Id = prevMedia.TvShows[i].Seasons[j].Episodes[k].Id;
                                this.tvShows[i].Seasons[j].Episodes[k].Name = prevMedia.TvShows[i].Seasons[j].Episodes[k].Name;
                                this.tvShows[i].Seasons[j].Episodes[k].Backdrop = prevMedia.TvShows[i].Seasons[j].Episodes[k].Backdrop;
                                this.tvShows[i].Seasons[j].Episodes[k].Date = prevMedia.TvShows[i].Seasons[j].Episodes[k].Date;
                                this.tvShows[i].Seasons[j].Episodes[k].Overview = prevMedia.TvShows[i].Seasons[j].Episodes[k].Overview;
                                this.tvShows[i].Seasons[j].Episodes[k].Path = prevMedia.TvShows[i].Seasons[j].Episodes[k].Path;
                            }
                        }

                    }
                }
            }
        }
    }

    class Movie
    {
        private int id;
        private string name;
        private string path;
        private string poster;
        private string backdrop;
        private string overview;
        DateTime? date;

        public Movie(string n, string p)
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

        internal bool Compare(Movie localMovie)
        {
            if (!this.name.Equals(localMovie.Name)) return false;
            if (!this.path.Equals(localMovie.Path)) return false;

            return true;
        }
    }

    class TvShow
    {
        private int id;
        private string name;
        private string overview;
        private string backdrop;
        private string poster;
        private Season[] seasons;
        DateTime? date;

        public TvShow(string n)
        {
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

        public Season[] Seasons
        {
            get => seasons;
            set => seasons = value;
        }

        internal bool Compare(TvShow localShow)
        {
            if (!this.name.Equals(localShow.name)) return false;
            if (this.seasons.Length != localShow.seasons.Length) return false;
            for (int i = 0; i < this.seasons.Length; i++)
            {
                if (!this.seasons[i].Compare(localShow.seasons[i])) return false;
            }
            return true;
        }
    }

    class Season
    {
        private int id;
        private Episode[] episodes;
        private string poster;
        DateTime? date;

        public Season(int i)
        {
            id = i;
        }

        public int Id
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

        public Episode[] Episodes
        {
            get => episodes;
            set => episodes = value;
        }

        internal bool Compare(Season localSeason)
        {
            if (this.episodes.Length != localSeason.episodes.Length) return false;

            for (int i = 0; i < this.episodes.Length; i++)
            {
                if (!this.episodes[i].Compare(localSeason.episodes[i])) return false;
            }
            return true;
        }
    }

    class Episode
    {
        private int id;
        private string name;
        private string backdrop;
        private string overview;
        private string path;
        DateTime? date;

        public Episode(int i, string n, string p)
        {
            id = i;
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

        public string Path
        {
            get => path;
            set => path = value;
        }

        internal bool Compare(Episode otherEpisode)
        {
            if (!this.name.Equals(otherEpisode.name)) return false;
            if (!this.id.Equals(otherEpisode.id)) return false;

            return true;
        }
    }
}
