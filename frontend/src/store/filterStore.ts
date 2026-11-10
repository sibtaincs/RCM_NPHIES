import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import dayjs from 'dayjs';

interface FilterState {
  /** Provider organization (hospital) id; undefined = all hospitals. */
  hospitalId?: string;
  /** ISO dates (yyyy-MM-dd), inclusive. */
  from?: string;
  to?: string;
  setHospital: (id?: string) => void;
  setRange: (from?: string, to?: string) => void;
}

/** Global hospital + date-range filter applied by every page (persisted). */
export const useFilterStore = create<FilterState>()(
  persist(
    (set) => ({
      hospitalId: undefined,
      from: dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
      to: dayjs().format('YYYY-MM-DD'),
      setHospital: (hospitalId) => set({ hospitalId }),
      setRange: (from, to) => set({ from, to }),
    }),
    { name: 'nphies-filters' },
  ),
);
