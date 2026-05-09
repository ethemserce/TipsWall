export interface Market {
  id: number;
  name: string;
  developer_name: string | null;
  has_winning_calculations: boolean | null;
  active: boolean;
  available_in_standard: boolean;
  available_in_premium: boolean;
}
