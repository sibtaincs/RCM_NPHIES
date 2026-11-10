import { Button, Upload } from 'antd';
import { PaperClipOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import type { Attachment } from '@/types/rcm';

const MAX_BYTES = 10 * 1024 * 1024;

function readBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result).split(',')[1] ?? '');
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

/** Converts picked files to NPHIES attachments (base64, max 10 MB each). */
export async function toAttachments(files: UploadFile[] = []): Promise<Attachment[]> {
  const result: Attachment[] = [];
  for (const f of files) {
    const raw = f.originFileObj as File | undefined;
    if (!raw) continue;
    if (raw.size > MAX_BYTES) throw new Error(`${raw.name} exceeds 10 MB`);
    result.push({
      contentType: raw.type || 'application/octet-stream',
      title: raw.name,
      data: await readBase64(raw),
      creation: new Date().toISOString(),
    });
  }
  return result;
}

interface Props {
  value?: UploadFile[];
  onChange?: (files: UploadFile[]) => void;
  label?: string;
}

/** Form-friendly file picker (files stay client-side until submit). */
export function AttachmentPicker({ value = [], onChange, label = 'Attach files' }: Props) {
  return (
    <Upload
      multiple
      fileList={value}
      beforeUpload={() => false}
      accept=".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx,.txt"
      onChange={({ fileList }) => onChange?.(fileList)}
    >
      <Button icon={<PaperClipOutlined />}>{label}</Button>
    </Upload>
  );
}
