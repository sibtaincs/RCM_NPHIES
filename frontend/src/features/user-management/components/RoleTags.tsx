import { Tag, Tooltip } from 'antd';
import type { AppRole } from '@/types/auth';

/** Consistent colour per role so the grid is scannable. */
const ROLE_COLORS: Record<string, string> = {
  Admin: 'red',
  RCMProcessor: 'geekblue',
  RCMViewer: 'blue',
  ClaimsProcessor: 'green',
  ClaimsReviewer: 'lime',
  TechnicalReviewer: 'cyan',
  MedicalReviewer: 'purple',
  Accountant: 'gold',
  PayerUser: 'magenta',
  ProviderUser: 'orange',
  ComplianceOfficer: 'volcano',
  Auditor: 'geekblue',
  User: 'default',
};

export function RoleTag({ role }: { role: string }) {
  return <Tag color={ROLE_COLORS[role] ?? 'default'}>{role}</Tag>;
}

/** Renders role tags, collapsing overflow into a "+N" tooltip. */
export function RoleTags({ roles, max = 3 }: { roles: AppRole[]; max?: number }) {
  if (!roles?.length) return <Tag>None</Tag>;

  const visible = roles.slice(0, max);
  const hidden = roles.slice(max);

  return (
    <>
      {visible.map((r) => (
   <RoleTag key={r} role={r} />
      ))}
      {hidden.length > 0 && (
        <Tooltip title={hidden.join(', ')}>
   <Tag>+{hidden.length}</Tag>
        </Tooltip>
      )}
    </>
  );
}
