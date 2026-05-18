// Long-form legal text kept out of the i18n JSON so the bundle isn't
// inflated by paragraph-length strings that the editor never paginates
// through. Returns a list of paragraphs the LegalPage renders straight
// into the ScrollView. Markdown-style headings (lines starting with #)
// are emphasised in the renderer.
//
// IMPORTANT: This content positions the app as a *statistics + tracker*,
// not a betting service. Wording is deliberately conservative — every
// sentence reinforces that the app does not place bets, does not accept
// money for play, and does not direct users to bookmakers. If you edit
// this file, keep that framing intact; the legal review of the app
// hinges on it.

export type LegalTopic = 'terms' | 'privacy' | 'kvkk';

export interface LegalDoc {
  title: string;
  lastUpdated: string;
  paragraphs: string[];
}

const LAST_UPDATED = '18.05.2026';

const TERMS_TR: LegalDoc = {
  title: 'Kullanım Koşulları',
  lastUpdated: LAST_UPDATED,
  paragraphs: [
    '# 1. Hizmetin Tanımı',
    'TipsWall, geçmiş futbol istatistiklerini analiz eden ve kullanıcının kendi tahminlerini kişisel olarak takip etmesine olanak tanıyan bir mobil uygulamadır. Uygulama bahis kabul etmez, para alıp yatırmaz, üçüncü taraf bahis sitelerine yönlendirme yapmaz.',
    '# 2. Yaş Sınırı',
    'TipsWall yalnızca 18 yaş ve üzeri kullanıcılara yöneliktir. Uygulamayı kullanarak 18 yaşından büyük olduğunu beyan etmiş olursun.',
    '# 3. Hesap Sorumluluğu',
    'Hesap bilgilerinin (kullanıcı adı, şifre) gizli tutulmasından kullanıcı sorumludur. Hesabında gerçekleşen tüm işlemler kullanıcının sorumluluğundadır. Şifreni başkasıyla paylaşma; hesap güvenliğini ihlal eden bir durum fark edersen derhal bildir.',
    '# 4. Veri ve Tahminlerin Niteliği',
    'Uygulamadaki HIT / ROI / IMP gibi metrikler geçmiş örneklem üzerinden hesaplanan istatistiklerdir. Hiçbir metrik gelecek sonucu için garanti niteliği taşımaz. Uygulamadaki bilgiler eğitim ve istatistiksel araştırma amacıyla sunulur; finansal tavsiye değildir.',
    '# 5. Premium Üyelik',
    'Premium üyelik isteğe bağlıdır. Ücretlendirme uygulama içi satın alma kanallarıyla (App Store / Google Play) işlenir. İptal ve iade hakları ilgili platformun politikalarına tabidir.',
    '# 6. Yasak Kullanımlar',
    'Uygulamayı yasal mevzuata aykırı amaçlarla, otomatik scraping veya reverse-engineering ile, başkasının hesabına yetkisiz erişim için, ya da uygulama içeriğini izinsiz çoğaltıp dağıtmak için kullanamazsın. Bu maddelere aykırı kullanım tespit edilirse hesap kapatılabilir.',
    '# 7. Hizmet Değişiklikleri',
    'TipsWall ekibi, uygulamadaki özellikleri herhangi bir zamanda değiştirme, ekleme veya kaldırma hakkını saklı tutar. Önemli değişiklikler kullanıcılara uygulama içi bildirim veya e-posta ile duyurulur.',
    '# 8. Sorumluluk Sınırı',
    'Uygulama "olduğu gibi" sunulur. Veri kaynağındaki gecikme, eksiklik veya hatalardan, ya da kullanıcının uygulamadaki bilgilere dayanarak aldığı kararlardan TipsWall sorumlu tutulamaz. Yasal düzenlemelerin izin verdiği azami sınırlar içinde tüm dolaylı zararlardan muafiyet bu maddeyle ifade edilir.',
    '# 9. Hukuk ve Yetkili Mahkeme',
    'Bu sözleşmeden doğan uyuşmazlıklarda Türkiye Cumhuriyeti hukuku uygulanır. İstanbul Mahkemeleri ve İcra Daireleri yetkilidir.',
    '# 10. İletişim',
    'Koşullara dair soruların için: ethemserce@gmail.com',
  ],
};

const PRIVACY_TR: LegalDoc = {
  title: 'Gizlilik Politikası',
  lastUpdated: LAST_UPDATED,
  paragraphs: [
    '# 1. Toplanan Veriler',
    'Hesap oluşturduğunda e-posta adresin, kullanıcı adın ve şifren (hash\'lenmiş halde) saklanır. Sosyal giriş kullanırsan sağlayıcının (Google / Apple) ilettiği e-posta ve doğrulanmış kimlik tokenı kaydedilir. Misafir kullanımda cihaza özel anonim bir kimlik (UUID) saklanır.',
    '# 2. Cihaz ve Kullanım Verisi',
    'Anonim kullanım istatistikleri (hangi ekran açıldı, kaç kez kullanıldı, hata logları) toplanır. Bu veriler kişisel kimlikle eşleştirilmez. Toplanmayı Ayarlar > Kullanım verisi paylaş seçeneği ile kapatabilirsin.',
    '# 3. Verinin Saklanması',
    'Veriler şifrelenmiş bağlantı (HTTPS) üzerinden iletilir. Sunucularda Avrupa\'da yerleşik bir veri merkezinde saklanır. Şifreler bcrypt benzeri tek yönlü algoritma ile hash\'lenir; düz şifrelere erişimimiz yoktur.',
    '# 4. Üçüncü Taraflarla Paylaşım',
    'Verilerini reklam ağlarına veya bahis platformlarına satmaz, kiralamaz veya paylaşmayız. Yasal zorunluluk halinde (mahkeme kararı vb.) yetkili merciler dışında kimseyle paylaşılmaz.',
    '# 5. Hesap Silme ve Veri Kontrolü',
    'Ayarlar > Hesap > Hesabımı sil seçeneği ile hesabını silebilirsin. Hesap silme sonrası kişisel bilgilerin (e-posta, kullanıcı adı) anonimleştirilir; istatistiksel veriler hesap kimliğinden ayrılır. Soft-delete 30 gün sonra otomatik olarak kalıcı silmeye dönüşür.',
    '# 6. Çerez ve İzleme',
    'Mobil uygulama çerez kullanmaz. Anonim analytics için Sentry / first-party telemetry kullanılır; bu sistemler IP adresini saklamaz veya parmak izi yapmaz.',
    '# 7. Çocukların Gizliliği',
    'Uygulama 18 yaş altı kullanıma uygun değildir. Yanlışlıkla 18 yaş altı bir kullanıcıdan veri topladığımızı tespit edersek veri derhal silinir.',
    '# 8. Politika Değişiklikleri',
    'Bu politikada yapılacak önemli değişiklikler uygulama içi bildirimle duyurulur. Yürürlük tarihi politikanın en üstünde belirtilir.',
    '# 9. İletişim',
    'Veri ve gizlilikle ilgili her türlü soru için: ethemserce@gmail.com',
  ],
};

const KVKK_TR: LegalDoc = {
  title: 'KVKK Aydınlatma Metni',
  lastUpdated: LAST_UPDATED,
  paragraphs: [
    '# Veri Sorumlusu',
    'TipsWall (Ethem Serçe), 6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında "veri sorumlusu" sıfatıyla işlem yapar.',
    '# İşlenen Kişisel Veriler',
    'E-posta adresi, kullanıcı adı, hash\'lenmiş şifre, hesap oluşturma ve giriş zaman damgaları, cihaz tipi ve OS sürümü, uygulama kullanım olayları (ekran isimleri).',
    '# İşleme Amacı',
    'Hesap yönetimi, oturum açma, hizmet sunumu, güvenlik (hesap ele geçirme tespiti), istatistiksel kalite iyileştirme ve yasal yükümlülüklerin yerine getirilmesi.',
    '# Hukuki Sebep',
    'KVKK Madde 5/2 (c): "Sözleşmenin kurulması veya ifasıyla doğrudan doğruya ilgili olması"; Madde 5/2 (f): "İlgili kişinin temel hak ve özgürlüklerine zarar vermemek kaydıyla veri sorumlusunun meşru menfaatleri için gerekli olması".',
    '# Aktarımlar',
    'Veriler yurt içinde işlenir. Yurt dışına aktarım gerektiren hizmetler (Sentry hata izleme vb.) için açık rıza alınır. Reklam, bahis veya pazarlama amaçlı üçüncü taraflara aktarım yapılmaz.',
    '# Saklama Süresi',
    'Aktif hesap verisi, hesap kapanışına kadar saklanır. Hesap silinmesinden sonra kişisel veriler 30 gün içinde anonimleştirilir; yalnızca yasal saklama yükümlülüğüne tabi kayıtlar (vergi vb.) ilgili sürelerle sınırlı olarak tutulur.',
    '# Haklarınız',
    'KVKK Madde 11 kapsamında: (a) kişisel verilerinizin işlenip işlenmediğini öğrenme, (b) işlenmişse bilgi talep etme, (c) işlenme amacını ve amacına uygun kullanılıp kullanılmadığını öğrenme, (d) düzeltilmesini veya silinmesini isteme, (e) işleme itiraz etme haklarına sahipsiniz.',
    '# Başvuru',
    'Bu hakları kullanmak için ethemserce@gmail.com adresine yazılı başvuru yapabilirsiniz. Başvurular 30 gün içinde sonuçlandırılır.',
  ],
};

const DOCS_TR: Record<LegalTopic, LegalDoc> = {
  terms: TERMS_TR,
  privacy: PRIVACY_TR,
  kvkk: KVKK_TR,
};

const TERMS_EN: LegalDoc = {
  title: 'Terms of Use',
  lastUpdated: LAST_UPDATED,
  paragraphs: [
    '# 1. Service Description',
    'TipsWall is a mobile app that analyses historical football statistics and lets users track their own predictions personally. The app does not accept bets, does not handle money, and does not redirect users to third-party betting sites.',
    '# 2. Age Restriction',
    'TipsWall is intended for users 18 and older. By using the app you confirm you are over 18 years of age.',
    '# 3. Account Responsibility',
    'You are responsible for keeping your account credentials secure. All activity under your account is your responsibility. Do not share your password; report any breach to us immediately.',
    '# 4. Nature of Data and Predictions',
    'Metrics like HIT / ROI / IMP are computed from historical samples. No metric guarantees future outcomes. Information in the app is provided for educational and statistical research; it is not financial or betting advice.',
    '# 5. Premium Membership',
    'Premium membership is optional. Billing is handled by the in-app purchase channels (App Store / Google Play). Cancellation and refund rights follow the respective platform policies.',
    '# 6. Prohibited Use',
    'You may not use the app for unlawful purposes, automated scraping, reverse engineering, unauthorised access to other accounts, or unauthorised reproduction and distribution of app content. Violations may lead to account termination.',
    '# 7. Service Changes',
    'TipsWall reserves the right to modify, add, or remove features at any time. Material changes will be announced via in-app notice or email.',
    '# 8. Limitation of Liability',
    'The app is provided "as is". TipsWall is not responsible for delays, gaps, or errors in source data, or for decisions you make based on app information. To the maximum extent permitted by law, all indirect damages are disclaimed.',
    '# 9. Governing Law',
    'Disputes arising from these terms are governed by the laws of the Republic of Türkiye. Istanbul courts have jurisdiction.',
    '# 10. Contact',
    'Questions about these terms: ethemserce@gmail.com',
  ],
};

const PRIVACY_EN: LegalDoc = {
  title: 'Privacy Policy',
  lastUpdated: LAST_UPDATED,
  paragraphs: [
    '# 1. Data Collected',
    'When you create an account we store your email, username, and a hashed password. Social sign-in records the verified identity token from the provider (Google / Apple). Guest use stores an anonymous device UUID.',
    '# 2. Device and Usage Data',
    'Anonymous usage stats (which screens are opened, error logs) are collected. These are not tied to personal identity. You can disable collection via Settings > Share usage data.',
    '# 3. Data Storage',
    'Data is transmitted over encrypted (HTTPS) connections. Servers are hosted in an EU data centre. Passwords are hashed with a one-way algorithm; we never have access to plaintext passwords.',
    '# 4. Third-Party Sharing',
    'We do not sell, rent, or share your data with advertising networks or betting platforms. Disclosure to authorities only occurs under legal compulsion (court order etc.).',
    '# 5. Account Deletion and Data Control',
    'You can delete your account via Settings > Account > Delete account. After deletion, personal information is anonymised; statistical data is detached from your identity. Soft-delete becomes permanent after 30 days.',
    '# 6. Cookies and Tracking',
    'The mobile app uses no cookies. Anonymous analytics may use Sentry / first-party telemetry; these systems do not store IP addresses or fingerprint users.',
    '# 7. Children\'s Privacy',
    'The app is not intended for users under 18. If we learn we have collected data from a user under 18, the data is deleted promptly.',
    '# 8. Policy Changes',
    'Material changes to this policy are announced in-app. The effective date is shown at the top.',
    '# 9. Contact',
    'For data or privacy questions: ethemserce@gmail.com',
  ],
};

const KVKK_EN: LegalDoc = {
  title: 'Personal Data Notice (KVKK)',
  lastUpdated: LAST_UPDATED,
  paragraphs: [
    '# Data Controller',
    'TipsWall (Ethem Serçe) acts as the "data controller" under Turkish Personal Data Protection Law No. 6698 (KVKK).',
    '# Personal Data Processed',
    'Email address, username, hashed password, account creation and login timestamps, device type and OS version, app usage events (screen names).',
    '# Purpose of Processing',
    'Account management, authentication, service delivery, security (account takeover detection), statistical quality improvement, and legal compliance.',
    '# Legal Basis',
    'KVKK Article 5/2(c): "Directly related to the establishment or performance of a contract"; Article 5/2(f): "Necessary for legitimate interests of the data controller, provided that fundamental rights are not infringed".',
    '# Transfers',
    'Data is processed within Türkiye. Services that require cross-border transfer (e.g. Sentry error tracking) are used only with explicit consent. No transfer to advertising, betting, or marketing third parties.',
    '# Retention',
    'Active account data is kept until account closure. After deletion, personal data is anonymised within 30 days; only records subject to legal retention (tax etc.) are kept for the required durations.',
    '# Your Rights',
    'Under KVKK Article 11: (a) learn whether your data is processed, (b) request information if it is, (c) learn the purpose and confirm correct use, (d) request rectification or deletion, (e) object to processing.',
    '# How to Request',
    'To exercise these rights, send a written request to ethemserce@gmail.com. Requests are answered within 30 days.',
  ],
};

const DOCS_EN: Record<LegalTopic, LegalDoc> = {
  terms: TERMS_EN,
  privacy: PRIVACY_EN,
  kvkk: KVKK_EN,
};

export function getLegalDoc(topic: LegalTopic, lang: string | undefined): LegalDoc {
  return (lang ?? '').toLowerCase().startsWith('tr')
    ? DOCS_TR[topic]
    : DOCS_EN[topic];
}
