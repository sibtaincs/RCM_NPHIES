import { Tag } from 'antd';

/** Green "Active" / red "Inactive" pill. */
export function ActiveTag({ active }: { active?: boolean }) {
  return <Tag color={active ? 'green' : 'red'}>{active ? 'Active' : 'Inactive'}</Tag>;
}

/** Blue "Mapped" / default "Not mapped" pill for NPHIES mapping status. */
export function MappedTag({ mapped }: { mapped?: boolean }) {
  return <Tag color={mapped ? 'blue' : 'default'}>{mapped ? 'Mapped' : 'Not mapped'}</Tag>;
}
