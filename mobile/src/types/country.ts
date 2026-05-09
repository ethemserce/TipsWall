export interface Country {
  id: number;
  continent_id: number;
  name: string;
  official_name: string | null;
  iso2: string | null;
  iso3: string | null;
  image_path: string | null;
}
