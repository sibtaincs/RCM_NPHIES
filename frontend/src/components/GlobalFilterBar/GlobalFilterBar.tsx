import { DatePicker, Select, Space, Tooltip } from 'antd';
import { BankOutlined, LockOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useTranslation } from 'react-i18next';
import { useFilterStore } from '@/store/filterStore';
import { useHospitalLocked, useHospitals } from '@/hooks/useGlobalFilters';

const { RangePicker } = DatePicker;

/** Header bar: hospital picker + date range shared by every page. */
export function GlobalFilterBar() {
  const { t } = useTranslation();
  const { hospitalId, from, to, setHospital, setRange } = useFilterStore();
  const locked = useHospitalLocked();
  const { data: hospitals = [], isLoading } = useHospitals();

  const presets = [
    { label: t('filters.today'), value: [dayjs(), dayjs()] as [dayjs.Dayjs, dayjs.Dayjs] },
    { label: t('filters.last7'), value: [dayjs().subtract(7, 'day'), dayjs()] as [dayjs.Dayjs, dayjs.Dayjs] },
    { label: t('filters.last30'), value: [dayjs().subtract(30, 'day'), dayjs()] as [dayjs.Dayjs, dayjs.Dayjs] },
    { label: t('filters.thisMonth'), value: [dayjs().startOf('month'), dayjs()] as [dayjs.Dayjs, dayjs.Dayjs] },
    { label: t('filters.last90'), value: [dayjs().subtract(90, 'day'), dayjs()] as [dayjs.Dayjs, dayjs.Dayjs] },
    { label: t('filters.thisYear'), value: [dayjs().startOf('year'), dayjs()] as [dayjs.Dayjs, dayjs.Dayjs] },
  ];

  return (
    <Space wrap size="small">
      <Select
        style={{ minWidth: 240 }}
        loading={isLoading}
        allowClear={!locked}
        disabled={!!locked}
        showSearch
        optionFilterProp="label"
        suffixIcon={locked ? <LockOutlined /> : <BankOutlined />}
        placeholder={t('filters.allHospitals')}
        value={locked ?? hospitalId}
        onChange={(v) => setHospital(v)}
        options={hospitals.map((h) => ({
          value: h.id,
          label: h.licenseNumber ? `${h.name} (${h.licenseNumber})` : h.name,
        }))}
      />
      <Tooltip title={t('filters.dateHint')}>
        <RangePicker
          allowEmpty={[true, true]}
          presets={presets}
          value={[from ? dayjs(from) : null, to ? dayjs(to) : null]}
          onChange={(v) => setRange(v?.[0]?.format('YYYY-MM-DD'), v?.[1]?.format('YYYY-MM-DD'))}
        />
      </Tooltip>
    </Space>
  );
}
