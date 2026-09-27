ProcessVideoAnalyzer Release Package

실행 방법
1. GitHub Release에서 아래 분할 파일을 모두 같은 폴더에 내려받습니다.
   - ProcessVideoAnalyzer-v0.2.0-win-x64.tar.part01
   - ProcessVideoAnalyzer-v0.2.0-win-x64.tar.part02
   - ProcessVideoAnalyzer-v0.2.0-win-x64.tar.part03
2. Windows 명령 프롬프트에서 아래 명령으로 원본 tar 파일을 복원합니다.
   copy /b ProcessVideoAnalyzer-v0.2.0-win-x64.tar.part01+ProcessVideoAnalyzer-v0.2.0-win-x64.tar.part02+ProcessVideoAnalyzer-v0.2.0-win-x64.tar.part03 ProcessVideoAnalyzer-v0.2.0-win-x64.tar
3. 복원한 tar 파일을 압축 해제합니다.
   tar -xf ProcessVideoAnalyzer-v0.2.0-win-x64.tar
4. 압축 해제된 폴더에서 ProcessVideoAnalyzer.exe를 실행합니다.

체크섬 확인
- SHA256SUMS.txt 파일의 값과 다운로드한 파일의 SHA256 값이 일치하는지 확인하세요.

빌드 기준
- .NET 8
- Windows x64
- Local VLM / VLM Context Builder / Advanced Blocks 포함
