import { useEffect, useState } from 'react';
import { Alert, Col, DatePicker, Form, Input, Modal, Row, Select, Upload } from 'antd';
import type { UploadFile } from 'antd';
import { InboxOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import { useMutation } from '@tanstack/react-query';
import { documentsApi } from '@/api/endpoints/documents';
import type { ClaimDocument } from '@/types/documents';
import { EncounterSelect, PatientSelect, ProviderSelect, showError } from './shared';
import { ENCOUNTER_TYPES, MAX_UPLOAD_MB, fmtBytes, useDocumentTypes } from './documentUtils';

interface FormValues {
  providerId?: string;
  patientId?: string;
  encounterId?: string;
  encounterNumber?: string;
  encounterType?: string;
  invoiceNumber?: string;
  invoiceDate?: Dayjs;
  serviceDate?: Dayjs;
  documentType?: string;
  title?: string;
  notes?: string;
}

/**
 * Uploads a PDF/image into the claim document store. With `claimId` the document is attached to that
 * (editable) claim right away and patient / provider are taken from the claim.
 */
export function DocumentUploadModal({
  open,
  onClose,
  onUploaded,
  claimId,
  defaults,
}: {
  open: boolean;
  onClose: () => void;
  onUploaded: (doc: ClaimDocument) => void;
  claimId?: string;
  defaults?: Partial<FormValues>;
}) {
  const [form] = Form.useForm<FormValues>();
  const [files, setFiles] = useState<UploadFile[]>([]);
  const types = useDocumentTypes();
  const patientId = Form.useWatch('patientId', form);
  const encounterId = Form.useWatch('encounterId', form);

  useEffect(() => {
    if (open) {
      form.resetFields();
      form.setFieldsValue({ documentType: 'medical-report', ...defaults });
      setFiles([]);
    }
  }, [open]); // eslint-disable-line react-hooks/exhaustive-deps

  const file = files[0]?.originFileObj as File | undefined;
  const tooBig = !!file && file.size > MAX_UPLOAD_MB * 1024 * 1024;

  const upload = useMutation({
    mutationFn: (v: FormValues) =>
      documentsApi.upload(file!, {
        providerId: v.providerId,
        patientId: v.patientId!,
        encounterId: v.encounterId,
        encounterNumber: v.encounterNumber,
        encounterType: v.encounterType,
        invoiceNumber: v.invoiceNumber,
        invoiceDate: v.invoiceDate?.format('YYYY-MM-DD'),
        serviceDate: v.serviceDate?.format('YYYY-MM-DD'),
        documentType: v.documentType,
        title: v.title,
        notes: v.notes,
        claimId,
      }),
    onSuccess: (doc) => {
      onUploaded(doc);
      onClose();
    },
    onError: (e) => showError('Upload failed', e),
  });

  return (
    <Modal
      open={open}
      width={760}
      title={claimId ? 'Upload and attach document' : 'Upload document'}
      okText={claimId ? 'Upload & attach' : 'Upload'}
      okButtonProps={{ disabled: !file || tooBig, loading: upload.isPending }}
      onCancel={onClose}
      onOk={() => form.validateFields().then((v) => upload.mutate(v))}
      destroyOnClose
    >
      <Upload.Dragger
        accept=".pdf,.jpg,.jpeg,.png"
        maxCount={1}
        fileList={files}
        beforeUpload={() => false}
        onChange={({ fileList }) => setFiles(fileList.slice(-1))}
      >
        <p className="ant-upload-drag-icon">
          <InboxOutlined />
        </p>
        <p className="ant-upload-text">Click or drop a PDF / JPEG / PNG here</p>
        <p className="ant-upload-hint">Max {MAX_UPLOAD_MB} MB. Large PDFs are compressed when Ghostscript is configured.</p>
      </Upload.Dragger>
      {tooBig && <Alert type="error" showIcon style={{ marginTop: 8 }} message={`File is ${fmtBytes(file!.size)}; the maximum is ${MAX_UPLOAD_MB} MB.`} />}

      <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
        <Row gutter={12}>
          {!claimId && (
            <>
              <Col span={12}>
                <Form.Item name="providerId" label="Hospital / provider" rules={[{ required: true }]}>
                  <ProviderSelect />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="patientId" label="Patient" rules={[{ required: true }]}>
                  <PatientSelect />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="encounterId" label="Encounter" tooltip="Fills encounter number, type and service date from the encounter.">
                  <EncounterSelect patientId={patientId} />
                </Form.Item>
              </Col>
            </>
          )}
          <Col span={6}>
            <Form.Item name="encounterNumber" label="Encounter no.">
              <Input disabled={!!encounterId} placeholder={encounterId ? 'From encounter' : 'e.g. IP-2026-0012'} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="encounterType" label="Encounter type">
              <Select allowClear disabled={!!encounterId} options={ENCOUNTER_TYPES} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="invoiceNumber" label="Invoice no.">
              <Input />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="invoiceDate" label="Invoice date">
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="serviceDate" label="Service date" tooltip="Day of care the document belongs to; used to match interim claims of long stays.">
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="documentType" label="Document type" rules={[{ required: true }]}>
              <Select loading={types.isLoading} options={(types.data ?? []).map((t) => ({ value: t, label: t }))} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="title" label="Title" tooltip="Shown to the payer as the attachment title. Defaults to the file name.">
              <Input maxLength={200} />
            </Form.Item>
          </Col>
          <Col span={24}>
            <Form.Item name="notes" label="Notes">
              <Input.TextArea rows={2} maxLength={1000} />
            </Form.Item>
          </Col>
        </Row>
      </Form>
    </Modal>
  );
}
