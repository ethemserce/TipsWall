export interface League {
  id: number;
  sport_id: number;
  country_id: number | null;
  name: string;
  active: boolean;
  short_code: string | null;
  image_path: string | null;
  type: string | null;
  sub_type: string | null;
  category: number | null;
}
