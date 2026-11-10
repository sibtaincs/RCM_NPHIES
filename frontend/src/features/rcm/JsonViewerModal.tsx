import { Alert, Button, Empty, Modal, Space, Spin, Tabs, Tag, message } from 'antd';
import { CopyOutlined, DownloadOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { getApiErrorMessage } from '@/utils/apiError';
import { fmtDate } from '@/utils/display';

function JsonBlock({ json, fileName }: { json?: string | null; fileName: string }) {
  if (!json) return <Empty description="No JSON recorded" />;

  const copy = async () => {
    await navigator.clipboard.writeText(json);
    void message.success('Copied to clipboard');
  };
  const download = () => {
    const url = URL.createObjectURL(new Blob([json], { type: 'application/fhir+json' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <>
      <Space style={{ marginBottom: 8 }}>
        <Button size="small" icon={<CopyOutlined />} onClick={copy}>
          Copy
        </Button>
        <Button size="small" icon={<DownloadOutlined />} onClick={download}>
          Download
        </Button>
      </Space>
      <pre
        style={{
          maxHeight: '65vh',
          overflow: 'auto',
          margin: 0,
          padding: 12,
          fontSize: 12,
          background: 'rgba(0,0,0,0.04)',
          borderRadius: 6,
        }}
      >
        {json}
      </pre>
    </>
  );
}

/** Request / response FHIR JSON of a logged NPHIES transaction. */
export function TransactionJsonModal({
  transactionId,
  onClose,
}: {
  transactionId: string;
  onClose: () => void;
}) {
  const q = useQuery({
    queryKey: ['transaction-json', transactionId],
    queryFn: () => rcmApi.claims.transactionJson(transactionId),
  });
  const d = q.data;
  const base = d ? `${d.eventCode || d.operation}-${d.messageId}` : transactionId;

  return (
    <Modal
      open
      width="80%"
      footer={null}
      onCancel={onClose}
      title={d ? `${d.direction} ${d.operation} · ${d.messageId}` : 'NPHIES message'}
    >
      {q.isLoading && <Spin />}
      {q.error && <Alert type="error" message={getApiErrorMessage(q.error)} />}
      {d && (
        <>
          <Space wrap style={{ marginBottom: 8 }}>
            {d.source && <Tag color="blue">{d.source}</Tag>}
            {d.responseEventCode && <Tag>{d.responseEventCode}</Tag>}
            {d.httpStatus != null && <Tag>HTTP {d.httpStatus}</Tag>}
            {d.loggedAt && <span>{fmtDate(d.loggedAt, true)}</span>}
          </Space>
          {d.error && (
            <Alert type="warning" showIcon message={d.error} style={{ marginBottom: 8 }} />
          )}
          {!d.source ? (
            <Empty description="No audit record for this message (message audit may have been disabled)" />
          ) : (
            <Tabs
              defaultActiveKey={d.direction === 'inbound' ? 'response' : 'request'}
              items={[
                {
                  key: 'request',
                  label: d.source === 'poll-response' ? 'Request (poll)' : 'Request',
                  children: <JsonBlock json={d.requestJson} fileName={`${base}-request.json`} />,
                },
                {
                  key: 'response',
                  label: 'Response',
                  children: <JsonBlock json={d.responseJson} fileName={`${base}-response.json`} />,
                },
              ]}
            />
          )}
        </>
      )}
    </Modal>
  );
}

/** The request bundle the claim / prior-auth would send now (built server-side, not sent). */
export function BundlePreviewModal({ claimId, onClose }: { claimId: string; onClose: () => void }) {
  const q = useQuery({
    queryKey: ['bundle-preview', claimId],
    queryFn: () => rcmApi.claims.previewBundle(claimId),
    gcTime: 0,
  });
  const d = q.data;
  const problems = d?.warnings.filter((w) => w.startsWith('[Error]')) ?? [];
  const notes = d?.warnings.filter((w) => !w.startsWith('[Error]')) ?? [];

  return (
    <Modal
      open
      width="80%"
      footer={null}
      onCancel={onClose}
      title={
        d ? `Request JSON preview · ${d.claimNumber} (${d.operation})` : 'Request JSON preview'
      }
    >
      {q.isLoading && <Spin />}
      {q.error && (
        <Alert
          type="error"
          showIcon
          message="Cannot build the bundle"
          description={getApiErrorMessage(q.error)}
        />
      )}
      {d && (
        <>
          {problems.length > 0 && (
            <Alert
              type="error"
              showIcon
              style={{ marginBottom: 8 }}
              message={`${problems.length} validation error(s) — NPHIES would reject this`}
              description={
                <ul style={{ margin: 0 }}>
                  {problems.map((w) => (
                    <li key={w}>{w}</li>
                  ))}
                </ul>
              }
            />
          )}
          {notes.length > 0 && (
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 8 }}
              message={
                <ul style={{ margin: 0 }}>
                  {notes.map((w) => (
                    <li key={w}>{w}</li>
                  ))}
                </ul>
              }
            />
          )}
          {problems.length === 0 && (
            <Alert
              type="success"
              showIcon
              style={{ marginBottom: 8 }}
              message="Passes local NPHIES validation"
            />
          )}
          <JsonBlock json={d.json} fileName={`${d.claimNumber}-${d.operation}-request.json`} />
        </>
      )}
    </Modal>
  );
}
