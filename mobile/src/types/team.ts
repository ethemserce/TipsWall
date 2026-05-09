export interface Team {
  id: number;
  country_id: number | null;
  venue_id: number | null;
  name: string;
  short_code: string | null;
  image_path: string | null;
  founded: number | null;
  type: string | null;
  gender: string | null;
}
